# Check Version (cv)

Previous Names: Change Version

Check Version is a CLI tool that provides quick check against a repo's changes without saving any changed contents. It does so by recording only the update time. The outputs is just like `git status` - but without diff.

This is useful for cases when we DO NOT want to a full version control yet would still want the capability to see which files has changed, as in the case of multimedia projects (e.g. game projects).

## Publish

To publish, manually generate output: self-contained single file with trim but no aot (depends on yaml and uses reflection).

## Parcel NExT Dependencies

As of v1.1.1 we don't depend on Parcel NExT yet and should try to keep it that way.

## References

See respective project README for details.
See `CheckVersion/README.md` for more.