# Change Version (cv)

Change Version is a cli tool that provides quick check against a repo's changes without saving any changed contents. It does so by recording only the update time. The outputs is just like `git status`, without diff.

This is useful for cases when we DO NOT want to do version control yet would still want the capability to see which files has changed, as in the case of multimedia projects (e.g. game projects).

## Usage

The utility supports the following commands:

- `init`
- `status`
- `log`
- `commit -m <Message>`

## .cv

cv saves history inside the .cv folder. File update time is stored as utc.

## .gitignore

cv uses the same .gitignore file as git - but obviously the content is not saved.

At the moment cv just checks whether the beginning of paths match that as specified in .gitignore file - no wildcards is supported. You are welcome to make a PR in [this function](https://github.com/chaojian-zhang/cv/blob/91f711abcf1ba6d6a37ab8d3dc9c2d79ee694cc9/Program.cs#L344) to complete the implementation.

## Dependency and Platform

This program utilizes standard Net 6 and can be compiled to any native environment. The published releases only contains Windows-only builds for obvious reasons. To compile for other platforms, download .Net 6 SDK and build accordingly.
