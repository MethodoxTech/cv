# CheckVersion.GUI.UnitTests

Headless UI tests for `CheckVersion.GUI`.

Because the GUI is built procedurally with no XAML, nothing about the visual tree is verified at compile time — a control added to two parents, or a bad `Grid` row span, only fails at runtime. These tests boot the real `App` on Avalonia's headless platform and show the real `MainWindow`, so those failures surface in CI rather than on a user's desktop.

They also cover the view logic that has no counterpart in the CLI: the tracked-file list, the history list, the subfolder pick list, and the pack preview.

## Dependency

Uses `Avalonia.Headless.XUnit`, which is built on xunit v3 — hence this project references `xunit.v3` and is an `Exe`, unlike `CheckVersion.UnitTests` which stays on xunit 2.

No display server is required.

Rendering is pointed at Skia (`UseSkia()` with `UseHeadlessDrawing = false`) rather than the default headless drawing stub. The stub's text path stalls in shaping as soon as the output pane holds any text, which hangs the run rather than failing it; Skia is also what the desktop app uses, so the tests measure and lay out exactly what users see.

## Running

```text
dotnet test CheckVersion.GUI.UnitTests/CheckVersion.GUI.UnitTests.csproj
```
