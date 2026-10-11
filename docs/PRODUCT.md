# MarkdownViewr for Windows

Build a Windows counterpart to [Darin Kelkhoff's markdownViewr](https://github.com/darinkelkhoff/markdownViewr), using the KofTwentyTwo AppKit foundations and standards. The agreed display name is MarkdownViewr, repository MarkdownViewr.Windows, and permanent application ID markdownviewr-windows.

The first reader implementation opens Markdown with a Windows picker or literal launch argument, renders sanitized GFM and supports bounded local images, manual reload and automatic saves with relative scroll preservation. The original 20 document themes and persistent palette selection are implemented. Hosted reader/live-reload/theme selection and both installed update channels passed on 77dd04d. Editable custom themes, color/code-font/CSS controls and restoration are implemented; their dialog/browser acceptance passed in CI 38102867556. Frontmatter, resizable outline/depth/jump/section controls and literal source visibility/reload/physical resize passed CI 38106991467: 34 native/UI and published tests, 33 per installed channel, no skips. Paired browser/source find, repeated local-fragment reveal and configured editor launch passed CI 38110726381: 41 native/published tests and 40 per installed channel, no skips. Vim navigation passed CI 38112298176: 44 native/published tests and 43 per installed channel, zero skips. Parser-derived source positions, literal source folding and bidirectional heading-based scroll synchronization are implemented with 363 core tests/100% measured lines, including new per-window reading zoom. Named editor lists, stable migration, add/remove/reorder and file/folder choices are implemented. Current source synchronization, named editor lists and glyph/key/reload zoom pass `2639e2d` / CI38121762124 (49 native/published,48 per installed channel, no skips). Print-only theme/layout options, Windows print UI and PDF save are implemented with native/output acceptance pending; the previous source-provenance head b0f3b86 passed CI 38113042618, including published 44/44 and installed stable/dev 43/43 tests without skips. The full feature list below remains the target; the first reader is not a completed port.

## Reference and feature scope

Reference source inspected at commit `6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021`. Preserve Darin's MIT copyright/license when adapting his rendering resources or other code. macOS SwiftUI/WebKit and Sparkle integrations require Windows equivalents.

| Area | Required Windows behavior |
| --- | --- |
| Documents | Open Markdown through a picker and Windows file activation; multiple document windows; recent files; clear missing/unreadable-file errors |
| Rendering | GitHub Flavored Markdown, tables, tasks, strikethrough, fenced code, local images and links |
| Reload | Watch saves, including atomic replacements; debounce changes, preserve scroll, report deletion/recovery |
| Themes | Built-in themes, editable user themes, custom CSS, code font/size and persistent settings |
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
