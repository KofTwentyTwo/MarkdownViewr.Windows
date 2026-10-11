/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;


namespace MarkdownViewr.Windows;

/// <summary>Hosts passive Markdown without network access, authored scripts or native capabilities.</summary>
internal sealed partial class DocumentBrowser
{
   private const string SecurityHeaders = "Content-Security-Policy: default-src 'none'; img-src 'self'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\nCache-Control: no-store";
   private readonly WebView2 _view;
   private readonly Action<string> _status;
   private CoreWebView2Environment? _environment;
   private Core.Documents.DocumentResourcePolicy _policy = new();
   private DocumentFiles? _files;
   private byte[] _html = [];
   private Core.Documents.DocumentViewport _viewport;
   private ulong _navigationId;
   private bool _pageReady;
   private bool _restoreViewport;
   private bool _closed;
   private Core.Themes.ReaderTheme _theme;
   private Core.Themes.ReadingAppearance _appearance;
   private ulong _themeRevision;
   private ulong _appearanceRevision;
   private string _sanitizedHtml = "";
   private string _title = "MarkdownViewr";
   private double _zoomScale = 1;
   private IReadOnlyList<Core.Documents.DocumentHeading> _headings = [];
   private int[] _collapsedSections = [];



   /// <summary>Attaches a native status callback; the document itself cannot invoke host commands.</summary>
   public DocumentBrowser(WebView2 view, Action<string> status, Core.Themes.ReaderTheme theme, Core.Themes.ReadingAppearance? appearance = null)
   {
      _view = view;
      _status = status;
      _theme = theme;
      _appearance = (appearance ?? new Core.Themes.ReadingAppearance()).Sanitize();
   }



   /// <summary>Creates an isolated per-user browser profile and installs deny-by-default handlers before navigation.</summary>
   public async Task InitializeAsync()
   {
      _environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(AppIdentity.Paths.DataRoot, "WebView2"), null);
      if(_closed)
      {
         return;
      }
      await _view.EnsureCoreWebView2Async(_environment);
      if(_closed)
      {
         return;
      }
      CoreWebView2 core = _view.CoreWebView2;
      core.Settings.IsScriptEnabled = false;
      core.Settings.AreHostObjectsAllowed = false;
      core.Settings.IsWebMessageEnabled = false;
      core.Settings.AreDefaultScriptDialogsEnabled = false;
      core.Settings.AreDevToolsEnabled = false;
      core.Settings.AreDefaultContextMenusEnabled = false;
      core.Settings.AreBrowserAcceleratorKeysEnabled = false;
      core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
      core.DownloadStarting += (_, args) => args.Cancel = true;
      core.NewWindowRequested += (_, args) => args.Handled = true;
      core.FrameNavigationStarting += (_, args) => args.Cancel = true;
      core.NavigationStarting += NavigationStarting;
      core.NavigationCompleted += NavigationCompleted;
      core.SourceChanged += SourceChanged;
      core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
      core.WebResourceRequested += ResourceRequested;
      Show(null, "<h1>MarkdownViewr</h1><p>Choose File → Open to read a Markdown document.</p>", "MarkdownViewr");
   }



   /// <summary>Replaces the document capability and navigates to an intercepted HTTPS response, with no HTTP server.</summary>
   public void Show(DocumentFiles? files, string sanitizedHtml, string title, Core.Documents.DocumentViewport viewport = default, IReadOnlyList<Core.Documents.DocumentHeading>? headings = null)
   {
      ResetFindForNavigation();
      headings ??= [];
      if(!_headings.SequenceEqual(headings))
      {
         _collapsedSections = [];
      }
      _headings = headings;
      _files = files;
      _sanitizedHtml = sanitizedHtml;
      _title = title;
      _viewport = viewport;
      _pageReady = false;
      _restoreViewport = true;
      _policy = new Core.Documents.DocumentResourcePolicy();
      _html = Encoding.UTF8.GetBytes(Core.Documents.DocumentPage.Build(sanitizedHtml, title, _theme, _appearance, _zoomScale));
      _view.CoreWebView2.Navigate(_policy.DocumentUri.AbsoluteUri);
   }



   /// <summary>Retains window-local font scale through reload/theme navigation and restores its reading/collapse state.</summary>
   public async Task ApplyZoomAsync(double scale)
   {
      if(!double.IsFinite(scale) || scale is < 0.3 or > 5)
      {
         throw new ArgumentOutOfRangeException(nameof(scale));
      }
      Core.Documents.DocumentViewport viewport = await CaptureViewportAsync();
      if(!_closed)
      {
         _zoomScale = scale;
         Show(_files, _sanitizedHtml, _title, viewport, _headings);
      }
   }



   /// <summary>Changes only the document appearance and preserves its current relative viewport.</summary>
   public async Task ApplyThemeAsync(Core.Themes.ReaderTheme theme)
   {
      ulong revision = ++_themeRevision;
      Core.Documents.DocumentViewport viewport = await CaptureViewportAsync();
      if(!_closed && revision == _themeRevision)
      {
         _theme = theme;
         Show(_files, _sanitizedHtml, _title, viewport, _headings);
      }
   }


   /// <summary>Applies only native reader-wide preferences using the guarded viewport/collapse/find navigation path.</summary>
   public async Task ApplyAppearanceAsync(Core.Themes.ReadingAppearance appearance)
   {
      ArgumentNullException.ThrowIfNull(appearance);
      appearance.Validate();
      ulong revision = ++_appearanceRevision;
      Core.Documents.DocumentViewport viewport = await CaptureViewportAsync();
      if(!_closed && revision == _appearanceRevision)
      {
         _appearance = appearance;
         Show(_files, _sanitizedHtml, _title, viewport, _headings);
      }
   }



   /// <summary>Reveals a heading's hidden ancestors before moving the actual reading viewport to it.</summary>
   public async Task JumpToHeadingAsync(int index)
   {
      if(index < 0 || index >= _headings.Count)
      {
         return;
      }
      await ExecuteDocumentScriptAsync(FormattableString.Invariant($"(()=>{{const h=document.getElementById('mv-heading-{index}'); if(h){{ for(let p=h.parentElement;p;p=p.parentElement) p.hidden=false; h.scrollIntoView({{block:'start'}}); }}}})()"));
   }



   /// <summary>Only a host-generated section body can be collapsed, leaving its own heading visible.</summary>
   public async Task ToggleSectionAsync(int index)
   {
      if(index < 0 || index >= _headings.Count)
      {
         return;
      }
      Uri document = _policy.DocumentUri;
      string? result = await ExecuteDocumentScriptAsync(FormattableString.Invariant($"(()=>{{const e=document.querySelector('[data-mv-body=\"{index}\"]'); if(e) return e.hidden=!e.hidden;}})()"));
      if(document == _policy.DocumentUri && _findCollapsed is { } collapsed && result is "true" or "false")
      {
         _findCollapsed = string.Equals(result, "true", StringComparison.Ordinal) ? collapsed.Append(index).Distinct().ToArray() : collapsed.Where(value => value != index).ToArray();
      }
   }



   /// <summary>Applies an explicit native command to every generated section, including nested descendants.</summary>
   public async Task SetSectionsAsync(bool collapsed)
   {
      Uri document = _policy.DocumentUri;
      string? result = await ExecuteDocumentScriptAsync("for(const e of document.querySelectorAll('[data-mv-body]')) e.hidden=" + (collapsed ? "true" : "false") + ";");
      if(result is not null && document == _policy.DocumentUri && _findCollapsed is not null)
      {
         _findCollapsed = collapsed ? Enumerable.Range(0, _headings.Count).ToArray() : [];
      }
   }


   /// <summary>Fixed host operations run only on the ready intercepted document; content script execution remains disabled.</summary>
   private async Task<string?> ExecuteDocumentScriptAsync(string script)
   {
      if(_closed || !_pageReady || !IsCurrentDocument())
      {
         return null;
      }
      try
      {
         return await _view.CoreWebView2.ExecuteScriptAsync(script);
      }
      catch(COMException)
      {
         if(!_closed)
         {
            _status("The document navigation could not be completed.");
         }
         return null;
      }
   }


   /// <summary>Marks pending browser continuations inactive before closing the native surface.</summary>
   public void Close()
   {
      _closed = true;
      _view.Close();
   }



   /// <summary>Queries only fixed viewport data on the current document; content scripts and messages remain disabled.</summary>
   public async Task<Core.Documents.DocumentViewport> CaptureViewportAsync()
   {
      Uri document = _policy.DocumentUri;
      ulong navigation = _navigationId;
      if(!_closed && _pageReady && IsCurrentDocument())
      {
         string result = await _view.CoreWebView2.ExecuteScriptAsync("(()=>{const s=document.getElementById('content');return s?[s.scrollLeft/Math.max(1,s.scrollWidth-s.clientWidth),s.scrollTop/Math.max(1,s.scrollHeight-s.clientHeight)]:[0,0];})()");
         if(!_closed && document == _policy.DocumentUri && navigation == _navigationId && IsCurrentDocument())
         {
            _viewport = Core.Documents.DocumentViewport.FromBrowserResult(result);
            string collapsed = await _view.CoreWebView2.ExecuteScriptAsync("Array.from(document.querySelectorAll('[data-mv-body]')).filter(e=>e.hidden).map(e=>Number(e.dataset.mvBody))");
            if(!_closed && document == _policy.DocumentUri && navigation == _navigationId && IsCurrentDocument())
            {
               _collapsedSections = (JsonSerializer.Deserialize<int[]>(collapsed) ?? []).Where(index => index >= 0 && index < _headings.Count).Distinct().ToArray();
            }
         }
      }
      return _viewport;
   }



   /// <summary>Restores scroll after the matching navigation completes, using validated invariant numeric arguments.</summary>
   private async void NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
   {
      if(_closed || args.NavigationId != _navigationId || !IsCurrentDocument())
      {
         return;
      }
      if(!args.IsSuccess)
      {
         _status("The document surface could not be displayed.");
         return;
      }
      if(!_restoreViewport)
      {
         return;
      }
      _restoreViewport = false;
      try
      {
         string collapsed = JsonSerializer.Serialize(_collapsedSections);
         await sender.ExecuteScriptAsync("for(const e of document.querySelectorAll('[data-mv-body]')) e.hidden=" + collapsed + ".includes(Number(e.dataset.mvBody));");
         string script = FormattableString.Invariant($"if(location.href.split('#')[0] === '{_policy.DocumentUri.AbsoluteUri}') {{ const s=document.getElementById('content'); if(s) s.scrollTo({_viewport.HorizontalFraction:R}*Math.max(0,s.scrollWidth-s.clientWidth),{_viewport.VerticalFraction:R}*Math.max(0,s.scrollHeight-s.clientHeight)); }}");
         await sender.ExecuteScriptAsync(script);
         if(!_closed && args.NavigationId == _navigationId && IsCurrentDocument())
         {
            _pageReady = true;
            DocumentReady?.Invoke(this, EventArgs.Empty);
         }
      }
      catch(COMException)
      {
         if(!_closed)
         {
            _status("The document scroll position could not be restored.");
         }
      }
   }



   /// <summary>Native script calls must address the active intercepted document, including only an optional fragment.</summary>
   private bool IsCurrentDocument()
   {
      return Uri.TryCreate(_view.CoreWebView2.Source, UriKind.Absolute, out Uri? source) &&
         source.Query.Length == 0 &&
         string.Equals(source.GetLeftPart(UriPartial.Path), _policy.DocumentUri.AbsoluteUri, StringComparison.Ordinal);
   }



   /// <summary>Same-document fragment changes do not raise NavigationStarting; reveal only a validated generated target.</summary>
   private async void SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
   {
      if(_closed || args.IsNewDocument || !Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri? uri))
      {
         return;
      }
      if(_policy.GetHeadingIndex(uri, _headings.Count) is { } heading)
      {
         Uri document = _policy.DocumentUri;
         await JumpToHeadingAsync(heading);
         if(document == _policy.DocumentUri)
         {
            // Rearm an identical link after the user collapses its target again; keep the displayed viewport.
            await ExecuteDocumentScriptAsync($"if(location.href.split('#')[0] === '{document.AbsoluteUri}') history.replaceState(null,'','{document.AbsoluteUri}');");
         }
      }
   }


   /// <summary>Allows only the current document and fragment navigation; external links require an actual user gesture.</summary>
   private async void NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
   {
      if(_closed)
      {
         args.Cancel = true;
         return;
      }
      if(Uri.TryCreate(args.Uri, UriKind.Absolute, out Uri? uri) &&
         string.Equals(uri.GetLeftPart(UriPartial.Path), _policy.DocumentUri.AbsoluteUri, StringComparison.Ordinal) &&
         uri.Query.Length == 0)
      {
         if(args.IsUserInitiated && _policy.GetHeadingIndex(uri, _headings.Count) is { } heading)

         {
            args.Cancel = true;
            await JumpToHeadingAsync(heading);
            return;
         }
         _navigationId = args.NavigationId;
         return;
      }
      args.Cancel = true;
      if(args.IsUserInitiated && uri is not null && uri.UserInfo.Length == 0 &&
         !string.Equals(uri.Host, _policy.DocumentUri.Host, StringComparison.OrdinalIgnoreCase) && uri.Scheme is "https" or "http")
      {
         try
         {
            if(!await global::Windows.System.Launcher.LaunchUriAsync(uri))
            {
               _status("Windows could not open this link.");
            }
         }
         catch(Exception exception) when(exception is ArgumentException or COMException)
         {
            _status("Windows could not open this link.");
         }
      }
      else
      {
         _status("This link is outside the current document.");
      }
   }



   /// <summary>Every browser resource receives either the document, a verified local image, or an empty denial response.</summary>
   private void ResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
   {
      byte[] bytes = [];
      string type = "text/plain";
      int status = 403;
      if(string.Equals(args.Request.Method, "GET", StringComparison.Ordinal) && Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? uri))
      {
         if(uri == _policy.DocumentUri && args.ResourceContext == CoreWebView2WebResourceContext.Document)
         {
            bytes = _html;
            type = "text/html; charset=utf-8";
            status = 200;
         }
         else if(_files is not null && args.ResourceContext == CoreWebView2WebResourceContext.Image &&
                 _policy.GetImageRelativePath(uri) is string path)
         {
            try
            {
               bytes = _files.ReadImage(path);
               type = Path.GetExtension(path).ToUpperInvariant() switch
               {
                  ".SVG" => "image/svg+xml",
                  ".JPG" or ".JPEG" => "image/jpeg",
                  ".PNG" => "image/png",
                  ".GIF" => "image/gif",
                  ".WEBP" => "image/webp",
                  ".BMP" => "image/bmp",
                  ".AVIF" => "image/avif",
                  _ => "image/x-icon",
               };
               status = 200;
            }
            catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
               _status("One or more local images could not be loaded.");
            }
         }
      }
      var stream = new MemoryStream(bytes, writable: false);
      args.Response = _environment!.CreateWebResourceResponse(stream.AsRandomAccessStream(), status, status == 200 ? "OK" : "Forbidden", $"Content-Type: {type}\r\n{SecurityHeaders}");
   }
}
