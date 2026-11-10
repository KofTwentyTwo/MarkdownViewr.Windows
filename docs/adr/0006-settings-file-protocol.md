# ADR-0006: Settings file protocol

- **Status:** Accepted
- **Date:** 2026-11-10

## Context

One `settings.json` per edition is shared by every reader window and by every
process that uses the same data folder: an installed copy and a portable copy, or
two builds started from different folders, are separate processes over one file.

AppKit's `SettingsStore` returns defaults whenever the file cannot be read and writes
a whole snapshot through one fixed temporary name with no lock. With real files and
real processes this lost data in two ways: a load during a sharing violation returned
defaults that the next save wrote back, and two processes saving in turn erased each
other's changes.

The files beside `settings.json` are now a contract between application versions that
may run at the same time, so the protocol is recorded here.

## Decision

`AppSettingsStore` in the core library owns the file. AppKit still defines the shell
preferences (`ShellSettings`); only persistence moved.

**Change, not snapshot.** `Update` takes a function from the stored settings to the
new settings. It runs under the lock against what is on disk at that moment, so a
caller cannot write back a stale copy of a value it did not change.

**Lock.** `settings.json.lock` is opened with no sharing for the duration of an
update. It is never deleted: deleting a lock file leaves a window in which a second
opener is refused with access denied. The operating system releases the handle if
the process dies. A lock that stays held for every attempt refuses the update.

**Read.** The file is opened for reading while denying writers. A program that is
rewriting the file in place therefore reads as busy, not as invalid content, and
during an update no such program can start until the swap has happened. Readers
allow the file to be replaced, so a read never blocks a save.

**What a read can find.**

| Found | Used for display | On update |
| --- | --- | --- |
| No file | Defaults | The file is created |
| Valid settings | The stored values | Replaced |
| Some invalid values | Valid values kept, invalid ones as defaults | Replaced; the original is kept aside |
| Not settings at all | Defaults | Replaced; the original is kept aside |
| Unreadable or being written | Defaults | Refused; nothing is written |

A trailing comma, a comment and a quoted number are valid: they are what a hand edit
commonly leaves behind. A file over 16 MiB is not loaded.

**Write.** The new content is written to `settings.json.<32 hex digits>.tmp`,
flushed to disk, and swapped in with a replace operation, which also moves an invalid
original to its kept-aside name in the same step. If the file did not exist when it
was read, the temporary file is renamed into place without overwriting. A failed
update leaves the stored file as it was and removes its temporary file.

**Kept-aside files.** `settings.json.corrupt`, then `settings.json.corrupt-2` to
`settings.json.corrupt-9`; the first free name is used, and the last is reused once
all exist. Nothing reads them back; they exist so a hand edit gone wrong can be
recovered.

**Abandoned temporary files.** A process killed between its write and its swap
leaves a temporary file. The next update, holding the lock, removes files that match
this store's own name pattern and no others.

**Waiting.** Only a sharing violation is waited out: at most 20 attempts, 50 ms
apart, for the lock, the read and the swap. Access denied is treated as permanent and
reported at once, so a read-only profile does not stall a window.

**Reporting.** Faults are reported by kind with the exception type and HRESULT. No
path and no operating-system message text reaches the log.

## Consequences

- An update is a synchronous call on the window thread. It normally takes a few
  milliseconds and at most about one second per contended step.
- The JSON written is byte-compatible with earlier releases, so a downgrade reads it.
- An earlier release running against the same folder neither takes the lock nor
  denies writers. While it is mid-save, this store reads the file as busy and refuses
  the update; while this store holds the file, the earlier release's save fails and
  it reports that the setting could not be saved. Neither loses stored data, but the
  two can refuse each other until both are the same version.
- The store keeps no in-memory copy. Windows hold their own view of the settings and
  merge only the fields they changed (ADR-0004).

## Options considered

- **A named mutex instead of a lock file.** Rejected: a session-local name does not
  cover two sessions of one user on the same profile, and a global name can be
  created first by another account on the machine.
- **Fixing AppKit's store instead.** Still worthwhile for other applications, but the
  read-modify-write API is a different shape, and this application needed the fix
  without waiting for a package release.
