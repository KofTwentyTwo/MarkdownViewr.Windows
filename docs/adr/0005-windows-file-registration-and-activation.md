# ADR-0005: Windows file registration and activation

**Status:** Implemented locally; isolated boundary tests pass; native/installed acceptance pending
**Date:** 2026-10-11
**Decider:** James Maes, repository maintainer

## Context

The Windows counterpart must open Markdown/plain-text files from Explorer and
subsequent launches without replacing existing readers. Stable, development,
portable previews and isolated test profiles must retain their identity boundaries.
Relative arguments must keep the launching process's working-directory meaning.

## Decision

Use Velopack install/update/uninstall fast callbacks to register only installed
copies. The registered executable is the installation's stable
`current/MarkdownViewr.Windows.exe`. Advertise `.md`, `.markdown`, `.mdown`, `.mkd`
and `.txt` through per-user ProgIDs, OpenWithProgids and Default Apps capabilities.
Quote the executable and literal `%1` argument. Set `AllowSilentDefaultTakeOver`
as REG_NONE; never write extension defaults, UserChoice or machine-wide keys.

A ProgID identifies channel and canonical executable path. Uninstall first
checks that its command remains unchanged, then removes only owned entries.
Another channel, another installation and deliberate user retargeting survive.
Builds and portable copies do not register file types.

Use Windows App SDK AppInstance to choose the primary for the executable and
profile directories. SDK rich file activation and ordinary command-line paths
become the same fixed native document request. Resolve relative paths before
crossing processes; the public SDK launch envelope cannot carry sender CWD.

Deliver requests through a current-user-only .NET named pipe. Accept only a
source-generated bounded schema: at most128 absolute paths, a1MiB frame and an
optional fixed invalid-path notification. Reject unknown fields; no command or
script operation exists. Connection/read/acknowledgement waits are bounded to
10seconds, with at most16 requests queued before XAML attaches its dispatcher.
Malformed/disconnected peers cannot stop later valid requests. App shutdown
cancels and disposes the listener. No dependencies or generic RPC layer are added.

The foreground launcher permits the primary to focus an existing document.
Dispatch all window creation on the native UI thread; duplicate paths include
pending initial loads and preserve watcher/source/find/zoom state. Empty launch
focuses an existing reader. An invalid startup path gets a fixed native message;
logs contain only failure kind/HRESULT.

## User-selected default file types

Settings → General exposes a native Choose default file types button. Read-only
checks require this installed executable's unchanged command and the exact owned
RegisteredApplications value. Portable/unregistered previews instead show install
or repair guidance and cannot launch the selection action. Nothing selects or
reclaims a default, and `.txt` is explicitly optional.

Windows 11 23H2+ uses `ms-settings:defaultapps?registeredAppUser=` plus the
URI-escaped registered value name (the installation/channel ProgID, not a display
name). Earlier builds use general Default Apps with instructions to search `.md`;
this conservatively includes Windows 11 builds predating the supported deep link.
The native action revalidates ownership at click time, handles launch failure with
manual OS guidance and logs only error kind/HRESULT.

General also reports the effective `.md` default using Windows' read-only
[AssocQueryStringW](https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw)
with [ASSOCSTR_PROGID](https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/ne-shlwapi-assocstr).
Compare the returned ProgID with this installation/channel and require unchanged
registration ownership. An unavailable query stays Unknown; registration alone
never means Default. The Unicode buffer is capped at1024 characters, input is
fixed `.md`, and no UserChoice/hash/default writes or competing-app paths are
read or displayed. Refresh when returning from Windows Settings or explicitly
through Refresh status. Seven real read-only/isolated adapter cases pass;
actual user-selected default/Explorer acceptance is still required. Document content has no route
to invoke it. Actual Windows chooser/selection and Explorer acceptance are pending.

## Evidence and remaining acceptance

Eleven actual isolated registry/pipe/path tests pass, including two Default Apps
routing/ownership cases retained in `artifacts/default-files-boundaries/`. They cover command/icon
quoting, all five extensions/capabilities, REG_NONE marker, preserved defaults,
channel/installation cleanup, retarget protection, sender-relative Unicode,
profile identities, startup queuing, acknowledgements, malformed recovery and
idempotent cancellation/rebinding. Red/fixed TRXs are retained under
`artifacts/activation-boundaries/`. These do not prove Explorer or XAML behavior.

Two real secondary-app/profile/duplicate/watcher cases compile but have not run.
Require controlled native/published/installed acceptance, actual install/update/
uninstall readbacks and Explorer Open With before closing issue #14. Production
signatures and release publication remain separate requirements.

## References

- [Microsoft: App SDK single-instance lifecycle](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance)
- [Microsoft: Rich activation](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation)
- [Microsoft: Launch Default Apps settings](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-default-apps-settings)
- [Microsoft: File association ProgIDs](https://learn.microsoft.com/en-us/windows/win32/shell/fa-progids)
- [Microsoft SDK activation implementation](https://github.com/microsoft/WindowsAppSDK/blob/main/dev/AppLifecycle/AppInstance.cpp)
- [Velopack application hooks](https://docs.velopack.io/reference/csharp/Velopack.VelopackApp)
