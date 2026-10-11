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
   private const string Styles = """
      :root { color-scheme: light dark; font: 16px/1.65 system-ui, sans-serif; }
      body { max-width: 900px; margin: 0 auto; padding: 32px; overflow-wrap: anywhere; }
      h1,h2,h3,h4,h5,h6 { line-height: 1.25; margin-top: 1.5em; }
      pre { padding: 16px; overflow: auto; background: light-dark(#f3f4f6,#22272e); border-radius: 6px; }
      code,kbd { font-family: Consolas, monospace; }
      table { border-collapse: collapse; } th,td { border: 1px solid #888; padding: 6px 12px; }
      blockquote { margin-left: 0; padding-left: 20px; border-left: 4px solid #888; }
      img { max-width: 100%; height: auto; } a { color: light-dark(#0969da,#58a6ff); }
      """;

   private readonly WebView2 _view;
   private readonly Action<string> _status;
   private CoreWebView2Environment? _environment;
   private Core.Documents.DocumentResourcePolicy _policy = new();
   private DocumentFiles? _files;
   private byte[] _html = [];



   /// <summary>Attaches a native status callback; the document itself cannot invoke host commands.</summary>
   public DocumentBrowser(WebView2 view, Action<string> status)
   {
      _view = view;
      _status = status;
   }



   /// <summary>Creates an isolated per-user browser profile and installs deny-by-default handlers before navigation.</summary>
   public async Task InitializeAsync()
   {
      _environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(AppIdentity.Paths.DataRoot, "WebView2"), null);
      await _view.EnsureCoreWebView2Async(_environment);
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
      core.NavigationCompleted += (_, args) =>
      {
         if(!args.IsSuccess)
         {
            _status("The document surface could not be displayed.");
         }
      };
      core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
      core.WebResourceRequested += ResourceRequested;
      Show(null, "<h1>MarkdownViewr</h1><p>Choose File → Open to read a Markdown document.</p>", "MarkdownViewr");
   }



   /// <summary>Replaces the document capability and navigates to an intercepted HTTPS response, with no HTTP server.</summary>
   public void Show(DocumentFiles? files, string sanitizedHtml, string title)
   {
      _files = files;
      _policy = new Core.Documents.DocumentResourcePolicy();
      _html = Encoding.UTF8.GetBytes($"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(title)}</title><style>{Styles}</style></head><body>{sanitizedHtml}</body></html>""");
      _view.CoreWebView2.Navigate(_policy.DocumentUri.AbsoluteUri);
   }



   /// <summary>Allows only the current document and fragment navigation; external links require an actual user gesture.</summary>
   private async void NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
   {
      if(Uri.TryCreate(args.Uri, UriKind.Absolute, out Uri? uri) &&
         string.Equals(uri.GetLeftPart(UriPartial.Path), _policy.DocumentUri.AbsoluteUri, StringComparison.Ordinal) &&
         uri.Query.Length == 0)
      {
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
