# MarkdownViewr for Windows

Build a Windows counterpart to [Darin Kelkhoff's markdownViewr](https://github.com/darinkelkhoff/markdownViewr), using the KofTwentyTwo AppKit foundations and standards. The agreed display name is MarkdownViewr, repository MarkdownViewr.Windows, and permanent application ID markdownviewr-windows.

The first reader implementation opens Markdown with a Windows picker or literal launch argument, renders sanitized GFM and supports manual reload and bounded local images. Desktop acceptance is pending. The full feature list below remains the target; the first reader is not a completed port.

## Reference and feature scope

Reference source inspected at commit `6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021`. Preserve Darin's MIT copyright/license when adapting his rendering resources or other code. macOS SwiftUI/WebKit and Sparkle integrations require Windows equivalents.

| Area | Required Windows behavior |
| --- | --- |
| Documents | Open Markdown through a picker and Windows file activation; multiple document windows; recent files; clear missing/unreadable-file errors |
| Rendering | GitHub Flavored Markdown, tables, tasks, strikethrough, fenced code, local images and links |
| Reload | Watch saves, including atomic replacements; debounce changes, preserve scroll, report deletion/recovery |
| Themes | Built-in themes, editable user themes, custom CSS, code font/size and persistent settings |
| Navigation | Resizable table of contents, depth control, heading jumps and collapsible sections |
| Source | Optional source pane, synchronized headings/scroll and collapse state |
| Find | Match counts, next/previous matches, search in both panes and vim-style shortcuts |
| Keys | j/k, Ctrl+d/u, gg/G, ]]/[[, /, n/N; Windows Ctrl-based equivalents for native commands |
| Frontmatter | Hide YAML, display metadata or render it as Markdown |
| Editor | Configurable external editor with safe literal file arguments |
| Print | Native printing, theme/layout options and restoration of screen state afterwards |
| Foundation | AppKit About, activity log, theme/settings, crash handling, stable/dev updates, tests and release controls |

## Proposed implementation for discussion

Use a WinUI 3 shell with a WebView2 document surface, a pure .NET core for document/rendering state, and thin Windows adapters for file activation, watching, editor launch and printing. Markdig and HtmlSanitizer implement the first GFM and passive-markup boundary; see ADR-0002. Reuse compatible original HTML/CSS/navigation resources where useful, retaining attribution and replacing the macOS message bridge.

Treat document Markdown, HTML, images, CSS, links and web messages as untrusted. Document content must not execute arbitrary script or gain access to native host commands. Local-resource access must remain bounded to explicitly allowed document resources; remote navigation and external editor launch require deliberate user actions. Define and test the renderer policy before loading user files.

## Acceptance

Require deterministic core tests and 100% measured core line coverage, zero-warning builds, formatting, real document UI acceptance, malicious-content tests, both installed update channels and verified final signed Windows artifacts. Existing scaffold tests prove only the foundation. Do not describe the viewer, public releases or production signing as complete until corresponding evidence exists.
