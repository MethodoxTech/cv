# Check Version GUI

Version: v1.2.0

`CheckVersion.GUI` (`cv-gui`) is a desktop front-end for the `cv` CLI, built on Avalonia 12.

It is a thin shell: every operation is a call into the same `CheckVersionTool` the CLI uses, so the GUI and the CLI cannot drift apart in behavior.

## Dependency and Platform

Targets `.NET 10` and Avalonia `12.1.1` (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`).

## No XAML

The entire UI is written in C#. There is no `.axaml` anywhere in the project, and `EnableDefaultAvaloniaItems` is off so the build never looks for any.

Consequences worth knowing:

```text
The Fluent theme is added in code:     Styles.Add(new FluentTheme())
The skin is a code-built Styles set:   Styles.Add(AppTheme.BuildStyles())
Layout is control construction:        new DockPanel { Children = { ... } }
There are no bindings and no MVVM:     controls are fields, refreshed directly
There is no XAML name scope:           tests reach controls through internal accessors
```

The tradeoff is that nothing about the visual tree is checked at compile time, so `CheckVersion.GUI.UnitTests` runs the real window on Avalonia's headless platform to confirm it constructs, shows and refreshes.

## Design

`AppTheme.cs` holds the palette, metrics and control styling; `Views/Ui.cs` holds the reusable pieces (cards, chips, empty states, list item templates, icons). Together they are the code-behind equivalent of a resource dictionary, and everything else in the project builds from them.

```text
Dark theme by default, so the console-style output pane and the app agree
One accent (blue) for primary actions, plus a color per change type
Fluent's hover and focus visuals restyled through template-part selectors
Icons are inline vector paths, so there is no icon-font dependency
Empty lists show a hint rather than a blank box
```

The change-type colors are shared by the card markers, the row dots and the header chips:

```text
New       green
Updated   amber
Moved     cyan
Deleted   red
```

## Usage

```text
cv-gui [repo folder]
```

The folder argument is optional; the working directory is used when it is omitted, so launching from inside a repo just works.

## Window

```text
Repo bar     Path box, Browse, Open, Refresh, Init Repo
Status       New / Updated / Moved / Deleted, plus the commit box
Tracked      All tracked files, filterable, with [Missing] markers
History      Commits, newest first
Pack         Gather, Archive, and checkpoint create/restore
Output log   The same colored transcript the CLI prints
```

Long operations run on a background thread with the window locked, so a gather over a large repo does not freeze the UI.

Reading a repo is a long operation too — the changelist walks every folder and the stored history has to be deserialized, which takes seconds once a repo holds tens of thousands of files — so opening, browsing to and refreshing a repo all read on a background thread as well. The window appears immediately, shows `Reading repo…` while the read runs, and stays interactive throughout; only the finished result is applied. Switching repos mid-read is safe: a read that has been superseded is discarded rather than applied over the newer one.

## Packing a subfolder

The Pack tab is the GUI equivalent of `cv archive --subfolder`.

Leave the scope empty to pack the whole repo. Type or pick a subfolder to pack only the tracked files under it — the box suggests every folder that actually contains tracked files. A preview line reports how many files are in scope and what an output path will look like, before anything is written.

`Keep full repo-relative paths in output` maps to `--full-paths`.

Checkpoints are whole-repo only, for the reason described in `CheckVersion/README.md`.

## Interaction Model

The tool's own output interface is implemented by `UiOutput`, which marshals to the UI thread and appends to the log pane.

`ICheckVersionOutput.Confirm` is answered without blocking: the window resolves the only question the tool asks (creating an empty commit) with a real dialog *before* starting the background operation, so nothing ever has to prompt from a worker thread.

## Notes and Limitations

Push/pull are not exposed yet; use the CLI for those.

The window shows one repo at a time and re-reads from disk on every refresh. There is no file-system watcher, so use Refresh after changing files outside the app.

A read is not cancellable: the repo buttons are disabled until it finishes, rather than the walk being interrupted part way.
