/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;


namespace MarkdownViewr.Windows;

/// <summary>Hosts passive Markdown without network access, authored scripts or native capabilities.</summary>
internal sealed class DocumentBrowser
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
   private string _sanitizedHtml = "";
   private string _title = "MarkdownViewr";



   /// <summary>Attaches a native status callback; the document itself cannot invoke host commands.</summary>
   public DocumentBrowser(WebView2 view, Action<string> status, Core.Themes.ReaderTheme theme)
   {
      _view = view;
      _status = status;
      _theme = theme;
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
      core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
      core.DownloadStarting += (_, args) => args.Cancel = true;
      core.NewWindowRequested += (_, args) => args.Handled = true;
      core.FrameNavigationStarting += (_, args) => args.Cancel = true;
      core.NavigationStarting += NavigationStarting;
      core.NavigationCompleted += NavigationCompleted;
      core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
      core.WebResourceRequested += ResourceRequested;
      Show(null, "<h1>MarkdownViewr</h1><p>Choose File → Open to read a Markdown document.</p>", "MarkdownViewr");
   }



   /// <summary>Replaces the document capability and navigates to an intercepted HTTPS response, with no HTTP server.</summary>
   public void Show(DocumentFiles? files, string sanitizedHtml, string title, Core.Documents.DocumentViewport viewport = default)
   {
      _files = files;
      _sanitizedHtml = sanitizedHtml;
      _title = title;
      _viewport = viewport;
      _pageReady = false;
      _restoreViewport = true;
      _policy = new Core.Documents.DocumentResourcePolicy();
      _html = Encoding.UTF8.GetBytes($"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(title)}</title><style>{_theme.Styles}</style></head><body>{sanitizedHtml}</body></html>""");
      _view.CoreWebView2.Navigate(_policy.DocumentUri.AbsoluteUri);
   }



   /// <summary>Changes only the document appearance and preserves its current relative viewport.</summary>
   public async Task ApplyThemeAsync(Core.Themes.ReaderTheme theme)
   {
      Core.Documents.DocumentViewport viewport = await CaptureViewportAsync();
      _theme = theme;
      if(!_closed)
      {
         Show(_files, _sanitizedHtml, _title, viewport);
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
         string result = await _view.CoreWebView2.ExecuteScriptAsync("[window.scrollX/Math.max(1,document.documentElement.scrollWidth-innerWidth),window.scrollY/Math.max(1,document.documentElement.scrollHeight-innerHeight)]");
         if(!_closed && document == _policy.DocumentUri && navigation == _navigationId && IsCurrentDocument())
         {
            _viewport = Core.Documents.DocumentViewport.FromBrowserResult(result);
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
         string script = FormattableString.Invariant($"if(location.href.split('#')[0] === '{_policy.DocumentUri.AbsoluteUri}') window.scrollTo({_viewport.HorizontalFraction:R}*Math.max(0,document.documentElement.scrollWidth-innerWidth),{_viewport.VerticalFraction:R}*Math.max(0,document.documentElement.scrollHeight-innerHeight));");
         await sender.ExecuteScriptAsync(script);
         if(!_closed && args.NavigationId == _navigationId && IsCurrentDocument())
         {
            _pageReady = true;
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
