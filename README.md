# Check Version (cv)

Version: v1.2.0
Previous Names: Change Version

Check Version is a CLI tool that provides quick check against a repo's changes without saving any changed contents. It does so by recording only the update time. The outputs is just like `git status` - but without diff.

This is useful for cases when we DO NOT want to a full version control yet would still want the capability to see which files has changed, as in the case of multimedia projects (e.g. game projects).

As of v1.2.0 there is also a desktop front-end, `cv-gui`, built on Avalonia. It is a thin shell over the same tool the CLI drives.

## Projects

```text
CheckVersion            The `cv` CLI, and the tool logic every other project uses
CheckVersion.GUI        The `cv-gui` desktop front-end (Avalonia, no XAML)
CheckVersion.Server     File hosting server for push/pull
CheckVersion.UnitTests  Tool tests
CheckVersion.GUI.UnitTests  Headless UI tests
```

## Publish

To publish, manually generate output: self-contained single file with trim but no aot (depends on yaml and uses reflection).

The GUI publishes the same way. Avalonia needs its own runtime assets, so publish it per-RID rather than as a portable build.

## Parcel NExT Dependencies

As of v1.2.0 we don't depend on Parcel NExT yet and should try to keep it that way.

## References

See respective project README for details.
See `CheckVersion/README.md` for more.