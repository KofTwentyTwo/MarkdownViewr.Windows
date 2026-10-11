# MarkdownViewr for Windows

Build a Windows counterpart to [Darin Kelkhoff's markdownViewr](https://github.com/darinkelkhoff/markdownViewr), using the KofTwentyTwo AppKit foundations and standards. The agreed display name is MarkdownViewr, repository MarkdownViewr.Windows, and permanent application ID markdownviewr-windows.

The reader implements passive GFM, bounded local images, save/replacement/deletion recovery, the original20 themes and editable overrides, frontmatter, outline/collapse, literal synchronized source, paired find, vim commands, named editors and per-window zoom. Clean baseline `300e145` / CI38126078284 passes52 native/published and51 installed stable/dev tests with no failures/skips, including original3 native printing/PDF cases. Both actual AppKit update/apply/restart probes pass; fixtures are unsigned.

Full `bf4fcdc` / CI38128311242 passes54 native/published and53 installed stable/dev tests, zero skips: all5 print cases, real font/padding/width/border/background/image output and preserved source/find/reading state. Twenty-four generated PDFs and both actual AppKit apply/restart probes are retained; fixtures remain unsigned. Multiple native document windows, modern multiple-file picking, bounded private recent files and sibling-safe settings merging are implemented locally with376 core tests/100% measured lines; hosted lifecycle acceptance remains pending. Windows associations/activation and the remaining reference features below are still required. This is not a completed port.

Windows file registration and activation are now implemented locally (ADR-0005):
installed channel/executable identities, preserved defaults, SDK primary selection
and current-user bounded literal-path delivery. Nine real isolated boundary tests
pass, and the corrected entry point publishes/packs with all 543 published files verified
byte-for-byte in both archives. Real Explorer/installed lifecycle and two prepared
secondary-process/profile/duplicate/watcher cases remain unverified; this does
not complete the port or establish production signatures.

## Reference and feature scope

Credit the original macOS app and Darin Kelkhoff in About and public documentation, linking to [his repository](https://github.com/darinkelkhoff/markdownViewr) and [website](https://kelkhoff.com/); retain distributed MIT notices. Windows issues/support must remain in the Windows repository.

Reference source inspected at commit `6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021`. Preserve Darin's MIT copyright/license when adapting his rendering resources or other code. macOS SwiftUI/WebKit and Sparkle integrations require Windows equivalents.

| Area | Required Windows behavior |
| --- | --- |
| Documents | Open `.md`, `.markdown`, `.mdown`, `.mkd` and plain text through a multiple-file picker and Windows activation; independent document windows; recent files/clear history; clear missing/unreadable-file errors |
| Rendering | GitHub Flavored Markdown, tables, tasks, strikethrough, fenced code, local images/links; independently configurable highlight, superscript, subscript and underline extensions |
| Reload | Watch saves, including atomic replacements; debounce changes, preserve scroll, report deletion/recovery |
| Themes | 20 built-ins; editable body/heading/code fonts, sizes and palette; theme/global CSS; content width; theme order, enable/disable/delete/restore and cycling; JSON import/export with overwrite/keep-both/skip conflicts; persistent settings |
| Navigation | Resizable table of contents, depth control, configurable wrapping or ellipsis/full-title tooltip, optional heading-depth markers, heading jumps, collapsible sections and per-window font zoom (30–500%, Ctrl+plus/minus/0) |
| Source | Optional source pane, synchronized headings/scroll and collapse state; theme-aware syntax highlighting; draggable split opening at50/50 (#16/#17) |
| Find | Match counts, next/previous matches, search in both panes and vim-style shortcuts |
| Keys | j/k, Ctrl+d/u, gg/G, ]]/[[, /, n/N; Windows Ctrl-based equivalents for native commands |
| Frontmatter | Hide YAML, display metadata or render it as Markdown |
| Editor | Configurable external editor with safe literal file arguments |
| Print | Native printing, theme/layout options, readable PDF export with optional PDF/folder opening, and restoration of screen state afterwards (#22/#23) |
| Toolbar/settings | Mac toolbar action parity (#18), a dedicated native Settings dialog for theme/appearance configuration (#21) |
| Foundation | AppKit About with original-author credit, repository/issues/community Help links, activity log, crash handling, stable/dev updates, tests and release controls |

## Proposed implementation for discussion

Use a WinUI 3 shell with a WebView2 document surface, a pure .NET core for document/rendering state, and thin Windows adapters for file activation, watching, editor launch and printing. Markdig and HtmlSanitizer implement the first GFM and passive-markup boundary; see ADR-0002. Reuse compatible original HTML/CSS/navigation resources where useful, retaining attribution and replacing the macOS message bridge.

Treat document Markdown, HTML, images, CSS, links and web messages as untrusted. Document content must not execute arbitrary script or gain access to native host commands. Local-resource access must remain bounded to explicitly allowed document resources; remote navigation and external editor launch require deliberate user actions. Define and test the renderer policy before loading user files.

## Acceptance

Require deterministic core tests and 100% measured core line coverage, zero-warning builds, formatting, real document UI acceptance, malicious-content tests, both installed update channels and verified final signed Windows artifacts. Existing scaffold tests prove only the foundation. Do not describe the viewer, public releases or production signing as complete until corresponding evidence exists.
