# MarkdownViewr for Windows

Build a Windows counterpart to [Darin Kelkhoff's markdownViewr](https://github.com/darinkelkhoff/markdownViewr), using the KofTwentyTwo AppKit foundations and standards. The agreed display name is MarkdownViewr, repository MarkdownViewr.Windows, and permanent application ID markdownviewr-windows.

The reader implements passive GFM, bounded local images, save/replacement/deletion recovery, the original20 themes and editable overrides, frontmatter, outline/collapse, literal synchronized source, paired find, vim commands, named editors and per-window zoom. Clean baseline `300e145` / CI38126078284 passes52 native/published and51 installed stable/dev tests with no failures/skips, including original3 native printing/PDF cases. Both actual AppKit update/apply/restart probes pass; fixtures are unsigned.

Broader actual PDF font/padding/find comparisons are pending on corrected `bf4fcdc` after hosted evidence showed rounded UIA text metrics cannot provide an exact multi-step font ratio. Real width/border/full-page PDF comparisons already pass. Multiple native document windows, modern multiple-file picking, bounded private recent files and sibling-safe settings merging are implemented locally with376 core tests/100% measured lines; hosted lifecycle acceptance remains pending. Windows associations/activation and the remaining reference features below are still required. This is not a completed port.

## Reference and feature scope

Reference source inspected at commit `6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021`. Preserve Darin's MIT copyright/license when adapting his rendering resources or other code. macOS SwiftUI/WebKit and Sparkle integrations require Windows equivalents.

| Area | Required Windows behavior |
| --- | --- |
| Documents | Open `.md`, `.markdown`, `.mdown`, `.mkd` and plain text through a multiple-file picker and Windows activation; independent document windows; recent files/clear history; clear missing/unreadable-file errors |
| Rendering | GitHub Flavored Markdown, tables, tasks, strikethrough, fenced code, local images/links; independently configurable highlight, superscript, subscript and underline extensions |
| Reload | Watch saves, including atomic replacements; debounce changes, preserve scroll, report deletion/recovery |
| Themes | 20 built-ins; editable body/heading/code fonts, sizes and palette; theme/global CSS; content width; theme order, enable/disable/delete/restore and cycling; JSON import/export with overwrite/keep-both/skip conflicts; persistent settings |
| Navigation | Resizable table of contents, depth control, heading jumps, collapsible sections and per-window font zoom (30–500%, Ctrl+plus/minus/0) |
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
