# ADR-0004: Native document windows and private recent files

**Status:** Proposed; implementation and core tests pass, hosted lifecycle acceptance pending
**Date:** 2026-10-11
**Deciders:** James Maes, repository maintainer

## Context

Pinned markdownViewr uses SwiftUI DocumentGroup for independent document windows,
file opening and recent documents. Its readable types include md/markdown/mdown/mkd
and plain text. Windows must preserve each reader's watcher, source/find/collapse
and font zoom when another document opens or closes. Use published AppKit0.1.0.

## Decision

Track strong references to native WinUI XAML MainWindow instances on the shared
UI thread; remove them on Closed. Each owns its browser, watcher, load scheduler,
keyboard hook, find/source/collapse and zoom. Pass the initial literal path to
that window rather than reading process arguments in every Loaded callback.
File/New window and Ctrl+N create a blank reader; Close window/Ctrl+W close only
that reader; File/Exit closes all. Close the shared activity log only when the
last document window closes. Show splash/startup update checks only once.

Use the current App SDK FileOpenPicker(AppWindow.Id) and PickMultipleFilesAsync,
including all five reference extensions. Reuse a blank window, focus an already
open canonical path within this process, or create a new document window.
Cancellation leaves readers intact; failed opens retain any existing reader.
Windows registration and cross-process activation redirection remain separate
required work; launching an executable with literal paths is not association proof.

Store at most20 successful absolute paths in the private AppKit settings file,
deduplicated with Windows path comparison. Update only after a parsed snapshot
commits. Clear history removes entries without deleting files or closing readers.
Keep paths out of activity logs/telemetry. Each preference save merges only fields
changed since that window's last successful save into the latest stored settings;
older window snapshots cannot resurrect cleared history or overwrite sibling edits.

## Options and consequences

Separate tracked XAML windows reuse existing document adapters and native lifetime
handling. Tabs would require additional detach/reattach state and do not supply the
reference's independent windows. Creating AppWindow directly does not attach XAML
content. Current duplication detection covers one process; OS activation and
stable/dev identity boundaries still need implementation and separate acceptance.

376 core tests pass with100% measured lines, including bounded/corrupt history,
case deduplication and sibling-safe settings merging. Require hosted real two-window
startup, actual independent glyph zoom, physical Ctrl+N/Ctrl+W, surviving save reload,
recent duplicate activation/missing errors, clear without stale resurrection and
Exit. Three new native cases are compiled; none has run on the local desktop.

## References

- [Microsoft: Multiple WinUI windows](https://learn.microsoft.com/en-us/windows/apps/develop/ui/multiple-windows)
- [Microsoft: App SDK multiple-file picker](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.fileopenpicker.pickmultiplefilesasync)
- [Microsoft: Single-instance activation lifecycle](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance)
