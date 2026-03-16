# Change Version (CLI)

Version: v1.0.4

**Dependency and Platform**

This program utilizes standard .Net 8 and can be compiled to any native environment. The published releases only contains Windows-only builds but may add Linux builds in the future. To compile for other platforms, download .Net 8 SDK and build accordingly. Publish AoT is expected in a future update.

## Usage

The utility supports the following version control commands:

- `init`
- `status`
- `log`
- `list`
- `commit -m <Message>`

Client/server commands:

- `push`
- `pull`

File operation commands:

- `gather <output folder>`
- `archive <output path>`

## Generated Folders & Files

Notice we could have adopted an opt-in scheme to make "just include those files" easier - but to be familiar with git, we chose an opt-out scheme (aka. ignore files), so let's stick with it.

### `.cv`

cv saves history inside the `.cv` folder. File update time is stored as utc.

### `.cvignore`

cv uses a `.cvignore` file, which shares the same `.gitignore` file format as `git`, this is used to decide which files cv should consider when issuing `status` command; Notice we are using a new file name instead of using .gitignore directly - this is to allow the same place having a git repo.

At the moment cv just checks whether the beginning of paths match that as specified in .cvignore file - no wildcards is supported. You are welcome to make a PR in [this function](https://github.com/chaojian-zhang/cv/blob/91f711abcf1ba6d6a37ab8d3dc9c2d79ee694cc9/Program.cs#L344) to complete the implementation.

## Examples

A typical ignore file:

```.cvignore
bin
obj
```

matches all of these:

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
/bin        # only root bin
build/*.log # match anywhere
/build/*.log
foo/**/bar
!important.txt
```

## References

* Official Wiki: https://wiki.methodox.io/en/Utilities/CLI/cv
* Build downloads: https://github.com/MethodoxTech/cv/releases

Changelog:

* v1.0.3: Modernize.
* v1.0.4: Add syncing functions.
* v1.0.5: Update ignore rule to ignore all matching folders. Fix serialization issue.