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

**Read for an update.** The file is opened for reading while denying writers, and
that handle is held until the rename. A program rewriting the file in place therefore
reads as busy, not as invalid content, and none can start between the read and the
rename. An empty file is waited on for the same attempts, because a program that
clears a file before rewriting it leaves it empty for a moment.

**Read for display.** The same open is tried first. If another handle has write
access, the file is read beside it instead of waiting, since such a handle is usually
idle. Content read that way which does not parse is retried and, if it never parses,
reported as unreadable rather than corrupt: it cannot be judged while it can still be
written. An empty file reads as defaults at once.

**What a read can find.**

| Found | Used for display | On update |
| --- | --- | --- |
| No file | Defaults | The file is created |
| Empty file | Defaults | Replaced after the wait; nothing kept aside |
| Valid settings | The stored values | Replaced |
| Some invalid properties | Valid properties kept, invalid ones as defaults | Replaced; a copy of the original is kept aside |
| Not settings at all | Defaults | Replaced; a copy of the original is kept aside |
| Unreadable or being written | Defaults | Refused; nothing is written |

A trailing comma, a comment and a quoted number are valid: they are what a hand edit
commonly leaves behind. Text the JSON reader cannot decode, such as an unpaired
surrogate escape, makes its property invalid. Repair works per top-level property:
one bad entry in a list costs that list. A file over 16 MiB is not loaded.

**Write.** The new content is written to `settings.json.<32 hex digits>.tmp` and
flushed to disk. That file is then renamed over `settings.json` in one step with the
Windows rename that replaces an open file (`FileRenameInfoEx` with POSIX semantics).
At no point is there no `settings.json`: a reader sees the old file or the new one,
and a process killed at any moment leaves one of them. Handles that were open on the
old file and allow deletion go on reading it. `File.Replace` is not used because it
is two renames with the file missing between them, and `File.Move` with overwrite is
not used because it refuses while any handle is open on the destination.

On a volume that does not implement that rename, the store closes its own read
handle and falls back to `File.Move` with overwrite. If the file did not exist when
it was read, the temporary file is renamed into place without overwriting. A failed
update leaves the stored file as it was and removes its temporary file.

**Kept-aside copies.** Before invalid content is replaced it is copied to
`settings.json.corrupt`, or to the first free of `settings.json.corrupt-2` to
`settings.json.corrupt-9`; the last name is overwritten once all exist. The copy is a
courtesy for recovering a hand edit: if it cannot be made the fault is reported and
the save proceeds, because refusing would block every later change too. A save that
then fails removes the copy it made. Nothing reads the copies back. They contain
whatever the file contained, including recent-document paths, so clearing recent
files deletes them.

**Abandoned temporary files.** A process killed between its write and its rename
leaves a temporary file. The next update, holding the lock, removes files that match
this store's own name pattern and no others.

**Waiting.** Only a sharing violation is waited out: at most 20 attempts, 50 ms
apart, for the lock, the read and the rename. Access denied is treated as permanent
and reported at once, so a read-only profile does not stall a window.

**Reporting.** Faults are reported by kind with the exception type and HRESULT. No
path and no operating-system message text reaches the log.

## Consequences

- An update is a synchronous call on the window thread. It normally takes a few
  milliseconds and at most about one second per contended step.
- The JSON written is byte-compatible with earlier releases, so a downgrade reads it.
- The rename needs Windows 10 version 1607 or later and a file system that supports
  it; NTFS does. Other volumes use the fallback, which is not safe against a kill
  between closing the file and the move completing.
- An earlier release running against the same data folder still has its own defect:
  it takes no lock, and when a read fails it falls back to defaults and may save
  them. This store cannot prevent that. It also makes that release's saves fail more
  often while both run, because the earlier release cannot replace a file this store
  has open. Run one version per data folder.
- The store keeps no in-memory copy. Windows hold their own view of the settings and
  merge only the fields they changed (ADR-0004).

## Options considered

- **A named mutex instead of a lock file.** Rejected: a session-local name does not
  cover two sessions of one user on the same profile, and a global name can be
  created first by another account on the machine.
- **Renaming the invalid file away instead of copying it.** Rejected: between that
  rename and the new file arriving there is no settings file, and a kill there loses
  the profile.
- **Fixing AppKit's store instead.** Still worthwhile for other applications, but the
  read-modify-write API is a different shape, and this application needed the fix
  without waiting for a package release.
