# README - TODO

Project wise TODO items is edited here to avoid clustering the public facing main README.

## TODO

Ignore File: (Expect another dedicated 3hr (without AI assistance) to get ignore working well)

- [x] Currently `obj`, `bin` etc. is not able to ignore such folders in subfolder paths.
    * Bare-name patterns match at any depth; covered by `IgnoreRuleTests`.
- [x] Support `.cvignore` in subfolders, git style.
    * See `IgnoreContext`/`IgnoreScope`. Deeper file wins, nested `!` can re-include, ignored folders are still never descended into.
- [ ] `cv status` performance with large folders sucks right now.
- [ ] Replace custom matching with FileSystemGlobbing: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher?view=net-9.0-pp and https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing
    * Note this would need to keep the layered nested-ignore semantics, which Matcher does not model directly.
- [ ] Include explicit .cvinclude file (also maps to Microsoft Matcher well), default * for all files.
- [ ] On Linux/macOS, dotfiles carry the Hidden attribute and are skipped by the folder walk, so `.cvignore` itself is not tracked there. Decide whether that should be special-cased — lifting the skip wholesale would start tracking `.git`.
- [ ] For the dev log, we could add a screenshot of CV status outputs for clarity.

File Operations:

- [x] Pack only a subfolder instead of the whole repo (`gather`/`archive` `--subfolder`).
- [ ] Decide whether a scoped checkpoint is worth supporting. It would need history rewriting (rebasing `.cv/versions` onto the subfolder), which is why it was left out.

GUI:

- [x] Avalonia front-end, `CheckVersion.GUI`, procedural (no XAML).
- [ ] Expose push/pull in the GUI.
- [ ] Watch the file system so the window refreshes without the Refresh button.
- [ ] Remember recently opened repos.

File synching:

- [ ] For push/pull, enable checking against server files and download only the needed files.

Server:

- [x] Create file hosting server ~~that understands CheckVersion file changes~~
    * A fully functional server just needs to keep files and serve as FTP (for the "current" files) and nothing more.
- [ ] Be able to check out latest and update remote folder with only needed files
- [x] Be able to sync to any local
    * Achieved through download endpoint on the server and `pull` command on client.
- [ ] Use MD5 as checksum for avoiding uploading/downloading the same files.

Remaining Issues:

- [x] Remove dependency on YamlDotNet and enable publish aot.
    - [x] ~~Replace with System.Text.Json that is AoT friendly, or a custom text format for human readability~~
    * No need to remove YamlDotNet since it can do static code generation for serialization types.
- [ ] Make sure we can publish Aot.

Aot:

- [ ] Make sure `System.Net.Http.Json` can publish aot.