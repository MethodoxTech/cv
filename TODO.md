# README - TODO

Add TODO file here to avoid the main README looking clustered.

## TODO

Ignore File: (Expect another dedicated 3hr (without AI assistance) to get ignore working well)

- [ ] Currently `obj`, `bin` etc. is not able to ignore such folders in subfolder paths.
- [ ] `cv status` performance with large folders sucks right now.
- [ ] Replace custom matching with FileSystemGlobbing: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher?view=net-9.0-pp and https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing
- [ ] Include explicit .cvinclude file (also maps to Microsoft Matcher well), default * for all files.
- [ ] For the dev log, we could add a screenshot of CV status outputs for clarity.

File synching:

- [ ] For push/pull, enable checking against server files and download only the needed files.

Server:

- [x] Create file hosting server ~~that understands cv file changes~~
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