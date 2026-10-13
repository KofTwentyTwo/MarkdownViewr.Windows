# Using MarkdownViewr

## Open and read a document

Choose **File → Open** or press **Ctrl+O**. You can open multiple documents in separate windows, return to recent files from the File menu, or open a file through Explorer after installing and selecting its association.

MarkdownViewr is a reader. Use **Open in editor** to edit a document in your configured external editor; the Source pane is read-only. Configure named editors in **File → Settings → Editors**. A disabled editor command explains what is missing when you hover over it.

## Contents, source, and search

- **Contents** shows document headings. Choose the heading depth in the pane, select a heading to jump, or collapse sections to concentrate on the rest.
- **Source** shows highlighted Markdown beside the rendered document. Each window starts with an equal split; drag the divider to resize. Focus the divider and use Left/Right, or Home to restore equal panes.
- **Ctrl+F** searches the document and source. Enter moves to the next match; Shift+Enter moves back; Escape closes search.
- Use the zoom buttons, **Ctrl++**, **Ctrl+-**, or **Ctrl+0** to adjust reading size or restore 100%.

Contents and Source are hidden in new windows by default. Set independent defaults in **Settings → General**; toolbar toggles affect the current window.

## Choose your appearance

The document palette and application light/dark appearance are separate choices. Use **Theme** in the toolbar to select a document palette. Use the View menu for app appearance and Markdown extension/frontmatter options.

**File → Settings** provides these pages:

| Page | Controls |
| --- | --- |
| General | Reading width and custom CSS, Contents presentation, new-window pane defaults, and default file types |
| Document themes | Palette selection, editing, management, and JSON import/export |
| Editors | Named external editors, order, and document/folder launch preferences |
| Printing | Saved print defaults |
| Advanced | Startup splash, splash duration, and startup update checks |

Immediate settings save as you change them. Custom CSS uses **Apply**; theme/editor drafts use **Save** or **Cancel**. The Printing page provides explicit save/restore actions. Theme editing is intended for your own trusted preferences.

## Follow file changes

Saving a file in another app refreshes both the rendered document and source automatically, while retaining your relative reading position. Replacement saves are supported.

If a file is deleted or cannot be read, the last loaded document stays visible. The footer indicates a stale snapshot or failed reload. Restore the file or choose **File → Reload** to retry. The footer also shows the loaded snapshot's modification time and **Open folder** for Explorer.

## Print or save a PDF

1. Select **Print** in the toolbar, **File → Print**, or **Ctrl+P**.
2. Choose print theme, image/background options, font scaling, width, borders, and page padding.
3. Select **Print...** to hand the document to the system print dialog, or **Save PDF...** and choose a destination.
4. For PDF output, enable the optional actions to open the PDF or its containing folder.

Cancelling leaves the reading view unchanged. Use **Settings → Printing** to save defaults for later jobs.

## Make it your default Markdown reader

This requires an installed edition; a portable copy does not register default-file choices.

1. Open **File → Settings → General → Choose default file types**.
2. In Windows Default Apps, select MarkdownViewr and assign the extensions you want.
3. Try opening a Markdown file from Explorer.

Supported choices are `.md`, `.markdown`, `.mdown`, and `.mkd`; `.txt` is optional. Depending on Windows version, the button opens this app's page or the general Default Apps page. You can also right-click a file, choose **Open with**, and select the installed viewer.

The installer preserves your current defaults. Windows requires you to choose the replacement.

## Updates and local data

Stable and development editions, including portable copies, update within their own channels using **Help → Check for updates**. They have separate settings, recent files, and logs.

Settings and logs are stored locally under `%LOCALAPPDATA%\markdownviewr-windows` or `%LOCALAPPDATA%\markdownviewr-windows-dev`. **Help → Activity log** opens the log viewer and provides a shortcut to its folder.

Documents are rendered locally. Authored scripts and remote images are blocked; local images must stay within the document folder. Following an external link is a deliberate action.

## Troubleshooting and support

- **The app cannot display a document:** check that the file still exists and is readable, then try **File → Reload**.
- **A remote image is absent:** remote image loading is blocked by the reader's document policy.
- **Open in editor is disabled:** hover for the reason, then configure an installed editor in Settings.
- **The viewer is not a default option:** use Setup rather than a portable copy, then reopen Windows Default Apps.
- **The app does not start:** confirm Microsoft Edge WebView2 Runtime is installed and inspect the activity logs if available.

For help, use [Discussions](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/discussions). Report reproducible problems through [Issues](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/issues/new/choose), including **Help → About** version, Windows version, and relevant sanitized logs. Report vulnerabilities privately using [SECURITY.md](../SECURITY.md).

## MSI installation

The MSI installs for the current Windows user, alongside the EXE and portable download options. Stable and dev have separate package identities. Use the same installer format for an existing edition; install the other channel separately to try previews. Uninstall an MSI installation through Windows Settings → Apps → Installed apps. The installer registers supported Markdown types and preserves your existing default choices.
