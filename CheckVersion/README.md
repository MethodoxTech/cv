# Check Version CLI

Version: v1.1.1

Check Version (`cv`) is a small file-version tracking CLI designed for simple local source snapshots. It tracks file paths, update times, creation times, file sizes, move/recreate/delete information, and commit history inside a local `.cv` folder.

It is intentionally lightweight and uses an opt-out tracking model: files are tracked by default unless excluded through `.cvignore`.

## Dependency and Platform

This project targets `.NET 10`.

The project is configured for single-file publishing and ReadyToRun publishing. Native AOT is currently disabled.

Published releases currently focus on Windows builds, but the tool can be built for other platforms with the appropriate .NET SDK and runtime identifier.

## Usage

```text
cv <command> [options]
```

Version control commands:

```text
cv init
cv status
cv list
cv commit -m <Message>
cv log
```

File operation commands:

```text
cv gather <output folder>
cv archive <output zip file>
```

Checkpoint commands:

```text
cv checkpoint create <target zip file>
cv checkpoint restore <source zip file>
```

Client/server commands:

```text
cv push <serverUrl> <apiKey>
cv pull <serverUrl> <apiKey>
```

General commands:

```text
cv help
cv -h
cv --help
cv version
cv -v
cv --version
```

## Commands

### `init`

Initializes a new Check Version repo in the current directory.

This creates a `.cv` folder and initializes the version history file.

```text
cv init
```

### `status`

Shows uncommitted changes.

```text
cv status
```

The status output groups files into:

```text
New
Updated
Moved
Deleted
```

A recreated file may appear as a delete plus a new/recreated entry.

### `list`

Lists all currently tracked files.

```text
cv list
```

If a tracked file is physically missing, it is shown as `[Missing]`.

The command also shows any uncommitted changes.

### `commit`

Creates a new commit from the current changes.

```text
cv commit -m "Message"
```

A commit records file metadata and change records in `.cv/versions`.

Important: normal commits do not store historical file contents. They record what changed, but they are not full restore points by themselves. Use `checkpoint create` when you need a restorable snapshot.

### `log`

Shows commit history.

```text
cv log
```

### `gather`

Copies all currently tracked files into an empty output folder while preserving directory structure.

```text
cv gather <output folder>
```

This does not copy `.cv` history.

If the repo has uncommitted changes, `gather` warns but continues. It copies the current contents of tracked files from disk. New untracked files are omitted.

### `archive`

Creates a zip archive containing all currently tracked files.

```text
cv archive <output zip file>
```

This does not include `.cv` history.

If the repo has uncommitted changes, `archive` warns but continues. It archives the current contents of tracked files from disk. New untracked files are omitted.

### `checkpoint create`

Creates a restorable checkpoint archive.

```text
cv checkpoint create <target zip file>
```

A checkpoint includes:

```text
.cv/versions
all currently tracked source files
```

The repo must be clean before creating a checkpoint. If there are uncommitted changes, the command reports an error and does not create the checkpoint.

Use this immediately after a commit when you want a portable restore point.

Typical flow:

```text
cv status
cv commit -m "Stable checkpoint"
cv checkpoint create ../project-checkpoint.zip
```

### `checkpoint restore`

Restores a checkpoint archive into a clean folder.

```text
cv checkpoint restore <source zip file>
```

The target folder must not already contain a `.cv` repo. It should be empty, except it may contain the checkpoint zip file itself.

After restore, the folder contains both the version history and the tracked source files, so `cv status` should be clean immediately after restore.

Typical flow:

```text
mkdir RestoredProject
cd RestoredProject
cv checkpoint restore ../project-checkpoint.zip
```

### `push`

Uploads new and updated files to a Check Version server.

```text
cv push <serverUrl> <apiKey>
```

Example:

```text
cv push https://localhost:5001 your-api-key
```

### `pull`

Downloads files from a Check Version server and overwrites local copies.

```text
cv pull <serverUrl> <apiKey>
```

Example:

```text
cv pull https://localhost:5001 your-api-key
```

## Generated Folders and Files

Check Version uses an opt-out tracking model, similar to Git. Files are considered trackable unless ignored by `.cvignore`.

This makes it easy to start tracking a folder without explicitly adding each file.

### `.cv`

Check Version stores repo history inside the `.cv` folder.

The main history file is:

```text
.cv/versions
```

File update times are stored in UTC.

### `.cvignore`

Check Version uses `.cvignore` to exclude files and folders from tracking.

The format is intentionally similar to `.gitignore`, but it is implemented by Check Version directly and should not be assumed to support every advanced Git ignore rule.

A separate `.cvignore` file is used instead of `.gitignore` so that a folder can be both a Git repo and a Check Version repo without mixing ignore rules.

Example:

```.cvignore
bin
obj
```

This matches paths such as:

```text
bin
bin/file.dll
src/bin
src/bin/file.dll
a/b/obj
a/b/obj/file.o
```

More supported syntax:

```.cvignore
/bin
build/*.log
/build/*.log
foo/**/bar
!important.txt
```

Supported behavior includes:

```text
# comment lines
!negation
* wildcard within one path segment
** wildcard across path segments
? single-character wildcard within one path segment
/pattern anchored to repo root
directory/ directory-only style patterns
```

## Notes and Limitations

Check Version is not a Git replacement. It is designed for simple local version tracking and lightweight source snapshots.

Regular commits store change metadata, not full file contents. To create a portable restore point, use:

```text
cv checkpoint create <target zip file>
```

Use `archive` or `gather` when you only need the currently tracked source files without version history.

Use `checkpoint create` when you need both the tracked files and `.cv` history.

## References

* Official Wiki: https://wiki.methodox.io/en/Utilities/CLI/cv
* Build downloads: https://github.com/MethodoxTech/cv/releases

## Changelog:

* v1.0.3: Modernize.
* v1.0.4: Add syncing functions.
* v1.0.5: Update ignore rule to ignore all matching folders. Fix serialization issue.
* v1.0.6: Improve `list`; Implement `gather` and `archive`.
* v1.1.0: Upgrade to .Net 10; Significantly improve performance on large folders.
* v1.1.1: Support `checkpoint` commands.