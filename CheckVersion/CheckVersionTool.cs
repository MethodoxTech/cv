using CheckVersion.Serialization;
using CheckVersion.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace CheckVersion
{
    public class CheckVersionTool
    {
        #region Properties
        public string RootPath { get; }
        public string RepoControlFolderName { get; }
        public string ChangelogFilePath { get; }
        public string IgnoreFilename { get; }
        /// <summary>
        /// Where progress and diagnostic text goes. Defaults to the terminal.
        /// </summary>
        public ICheckVersionOutput Output { get; }

        public CheckVersionTool(string repoRootPath, string repoControlFolderName, string repoStorageFilePath, string ignoreFilename, ICheckVersionOutput? output = null)
        {
            RootPath = repoRootPath;
            RepoControlFolderName = repoControlFolderName;
            ChangelogFilePath = repoStorageFilePath;
            IgnoreFilename = ignoreFilename;
            Output = output ?? ConsoleOutput.Instance;
        }
        #endregion

        #region Accessors
        /// <summary>
        /// Whether a CV repo currently exists at <see cref="RootPath"/>.
        /// </summary>
        public bool RepoExists
            => Directory.Exists(RepoControlFolderPath);
        private string RepoControlFolderPath
            => Path.IsPathRooted(RepoControlFolderName)
            ? Path.GetFullPath(RepoControlFolderName)
            : Path.GetFullPath(Path.Combine(RootPath, RepoControlFolderName));
        private string ChangelogFullFilePath
            => Path.IsPathRooted(ChangelogFilePath)
            ? Path.GetFullPath(ChangelogFilePath)
            : Path.GetFullPath(Path.Combine(RootPath, ChangelogFilePath));
        private string IgnoreFullFilePath
            => Path.IsPathRooted(IgnoreFilename)
            ? Path.GetFullPath(IgnoreFilename)
            : Path.GetFullPath(Path.Combine(RootPath, IgnoreFilename));
        /// <summary>
        /// Bare file name to look for when discovering nested ignore files during the folder walk.
        /// </summary>
        private string IgnoreFileNameOnly
            => Path.GetFileName(IgnoreFilename);
        private string ChangelogArchivePath
            => NormalizeArchivePath(Path.GetRelativePath(Path.GetFullPath(RootPath), ChangelogFullFilePath));
        #endregion

        #region Structured Queries
        /// <summary>
        /// Current uncommitted changes. Prefer this over parsing <see cref="Status"/> output.
        /// </summary>
        public Changelist GetChangelist()
            => GetChanges();
        /// <summary>
        /// Current uncommitted changes, measured against an already-loaded history.
        /// </summary>
        /// <remarks>
        /// Reading the stored history is the expensive half of every query here, so a host that shows
        /// several views of one repo state should load it once and pass it to each accessor rather than
        /// paying for a fresh deserialization per view.
        /// </remarks>
        public Changelist GetChangelist(RepoHistory history)
            => GetChanges(history);
        /// <summary>
        /// The commit history as stored on disk.
        /// </summary>
        public RepoHistory GetHistory()
        {
            if (!RepoExists)
                throw new InvalidOperationException("Must be inside a CV repo.");

            return SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
        }
        /// <summary>
        /// All currently tracked file paths, relative to the repo root, sorted.
        /// </summary>
        public List<string> GetTrackedFiles()
            => GetTrackedFiles(GetHistory());
        /// <summary>
        /// All currently tracked file paths from an already-loaded history, relative to the repo root, sorted.
        /// </summary>
        public static List<string> GetTrackedFiles(RepoHistory history)
            => [.. history.GetLatestFiles().Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
        /// <summary>
        /// Every folder that contains at least one tracked file, including intermediate folders, sorted.
        /// Handy for offering a pick list of pack-able subfolders.
        /// </summary>
        public List<string> GetTrackedFolders()
            => GetTrackedFolders(GetHistory());
        /// <summary>
        /// Every folder that contains at least one tracked file in an already-loaded history, sorted.
        /// </summary>
        public static List<string> GetTrackedFolders(RepoHistory history)
        {
            HashSet<string> folders = new(StringComparer.OrdinalIgnoreCase);
            foreach (string path in history.GetLatestFiles().Keys)
            {
                string current = path;
                while (true)
                {
                    int separator = current.LastIndexOf('/');
                    if (separator <= 0)
                        break;

                    current = current[..separator];
                    folders.Add(current);
                }
            }
            return [.. folders.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
        }
        #endregion

        #region Methods
        /// <summary>
        /// Log all existing commits.
        /// </summary>
        public void Log()
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
            for (int i = 0; i < storage.Commits.Count; i++)
            {
                RepoHistory.Commit commit = storage.Commits[i];
                Output.Write(Color.White, $"{i}.".PadRight(3));
                Output.Write(Color.Green, commit.Time.ToLocalTime().ToString() + " ");
                Output.WriteLine(Color.White, commit.Message);
            }
            Output.WriteLine(Color.Goldenrod, $"{storage.Commits.Count} {(storage.Commits.Count <= 1 ? "commit" : "commits")}.");
        }
        /// <summary>
        /// Commit current changes to the repo.
        /// </summary>
        public void Commit(string message)
        {
            if (!RepoExists)
                Output.WriteLine(Color.Red, "No repo exists at current location");
            else
            {
                Changelist changes = GetChanges();

                RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
                List<FileChangeRecord> allChanges = changes.DeletedFiles
                    .Union(changes.UpdatedFiles)
                    .Union(changes.MovedFiles)
                    .Union(changes.NewFiles) // Order matters, we must union DeletedFiles first because in the case of FileChangeType.Recreate, we want to maintain that relation
                    .ToList();

                if (allChanges.Count == 0
                    && !Output.Confirm("There is no changed file, are you sure you want to make an empty commit?", defaultAnswer: true))
                    return;

                storage.Commits.Add(new RepoHistory.Commit()
                {
                    Changes = allChanges,
                    Message = message,
                    Time = DateTime.Now.ToUniversalTime()
                });
                SerializationHelper.SerializeToFile(storage, ChangelogFullFilePath);
                Output.WriteLine(Color.Goldenrod, $"Saved {allChanges.Count} {(allChanges.Count <= 1 ? "file" : "files")}.");
            }
        }
        /// <summary>
        /// Initialize a new repo.
        /// </summary>
        public void Init()
        {
            if (RepoExists)
                Output.WriteLine(Color.Red, "A CV repo already exists at this location.");
            else
            {
                Directory.CreateDirectory(RepoControlFolderPath);
                SerializationHelper.SerializeToFile(new RepoHistory(), ChangelogFullFilePath);
                Output.WriteLine(Color.GreenYellow, $"Repo initialized at: {RootPath}");
            }
        }
        /// <summary>
        /// Print all the changes.
        /// </summary>
        public void Status()
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            Changelist changes = GetChanges();
            Output.WriteLine(Color.Goldenrod, $"# New: {changes.NewFiles.Count}");
            foreach (FileChangeRecord file in changes.NewFiles)
            {
                Output.Write(Color.Green, $"{file.Path} ");
                if (file.ChangeType == FileChangeRecord.FileChangeType.Recreated)
                {
                    Output.Write(Color.DarkGray, file.UpdateTime.ToLocalTime().ToString());
                    Output.WriteLine(Color.Yellow, " [Recreated]");
                }
                else
                    Output.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime().ToString());
            }

            Output.WriteLine(Color.Goldenrod, $"# Updated: {changes.UpdatedFiles.Count}");
            foreach (FileChangeRecord file in changes.UpdatedFiles)
            {
                Output.Write(Color.YellowGreen, $"{file.Path} ");
                Output.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime().ToString());
            }

            Output.WriteLine(Color.Goldenrod, $"# Moved: {changes.MovedFiles.Count}");
            foreach (FileChangeRecord file in changes.MovedFiles)
            {
                Output.Write(Color.SkyBlue, $"{file.Path} ");
                Output.Write(Color.Yellow, $"-> ");
                Output.Write(Color.SkyBlue, $"{file.NewPath} ");
                Output.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime().ToString());
            }

            Output.WriteLine(Color.Goldenrod, $"# Deleted: {changes.DeletedFiles.Count}");
            foreach (FileChangeRecord file in changes.DeletedFiles)
            {
                Output.Write(Color.DarkRed, $"{file.Path} ");
                Output.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime().ToString());
            }
        }
        /// <summary>
        /// List all tracked files. If a tracked file is physically missing, show [Missing].
        /// Also shows uncommitted changes.
        /// </summary>
        public void List()
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            // Load tracked files
            RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
            List<string> tracked = storage
                .GetLatestFiles()
                .Keys
                .OrderBy(p => p)
                .ToList();

            Output.WriteLine(Color.Cyan, "# Tracked files:");
            foreach (string path in tracked)
            {
                string fullPath = Path.Combine(RootPath, path);
                if (File.Exists(fullPath))
                    Output.WriteLine(Color.White, path);
                else
                {
                    Output.Write(Color.White, path);
                    Output.WriteLine(Color.Yellow, " [Missing]");
                }
            }

            // Compute any pending changes
            Changelist changes = GetChanges();
            if (HasUncommittedChanges(changes))
            {
                Output.WriteLine();
                Output.WriteLine(Color.Goldenrod, "# Uncommitted changes:");

                foreach (FileChangeRecord f in changes.NewFiles)
                    Output.WriteLine(Color.Green, $"New:     {f.Path}");
                foreach (FileChangeRecord f in changes.UpdatedFiles)
                    Output.WriteLine(Color.YellowGreen, $"Updated: {f.Path}");
                foreach (FileChangeRecord f in changes.MovedFiles)
                    Output.WriteLine(Color.SkyBlue, $"Moved:   {f.Path} → {f.NewPath}");
                foreach (FileChangeRecord f in changes.DeletedFiles)
                    Output.WriteLine(Color.DarkRed, $"Deleted: {f.Path}");
            }
        }
        /// <summary>
        /// Copy currently tracked files into an empty destination folder, preserving folder structure.
        /// </summary>
        /// <param name="subfolder">
        /// Optional repo-relative (or absolute, but inside the repo) folder. When given, only tracked files
        /// under that folder are gathered.
        /// </param>
        /// <param name="preserveRepoPaths">
        /// When a subfolder is given, keep the full repo-relative layout instead of making the subfolder
        /// the root of the output. Ignored when gathering the whole repo.
        /// </param>
        public void Gather(string outputFolder, string? subfolder = null, bool preserveRepoPaths = false)
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                Output.WriteLine(Color.Red, "Output folder is required.");
                return;
            }

            if (!TryResolveSubfolder(subfolder, out string subfolderPrefix))
                return;

            string fullOutputFolder = Path.GetFullPath(outputFolder);

            if (File.Exists(fullOutputFolder))
            {
                Output.WriteLine(Color.Red, "Output path points to a file, not a folder.");
                return;
            }

            if (Directory.Exists(fullOutputFolder))
            {
                bool isEmpty = !Directory.EnumerateFileSystemEntries(fullOutputFolder).Any();
                if (!isEmpty)
                {
                    Output.WriteLine(Color.Red, "Destination folder must be empty.");
                    return;
                }
            }
            else
            {
                Directory.CreateDirectory(fullOutputFolder);
            }

            Changelist changes = GetChanges();
            if (HasUncommittedChanges(changes))
                Output.WriteLine(Color.Yellow, "Warning: repo has uncommitted changes. Gather will copy current tracked file contents; new untracked files are omitted.");

            if (!TrySelectPackFiles(subfolderPrefix, "gather", out List<string> tracked))
                return;

            foreach (string relativePath in tracked)
            {
                string sourcePath = Path.Combine(RootPath, relativePath);
                string destinationPath = Path.Combine(fullOutputFolder, MapPackPath(relativePath, subfolderPrefix, preserveRepoPaths));

                string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                File.Copy(sourcePath, destinationPath, overwrite: false);
                Output.WriteLine(Color.Green, $"Gathered {relativePath}");
            }

            Output.WriteLine(Color.GreenYellow, $"Gather complete: {fullOutputFolder} ({tracked.Count} {(tracked.Count == 1 ? "file" : "files")})");
        }
        /// <summary>
        /// Archive currently tracked files into a zip file.
        /// </summary>
        /// <param name="subfolder">
        /// Optional repo-relative (or absolute, but inside the repo) folder. When given, only tracked files
        /// under that folder are archived.
        /// </param>
        /// <param name="preserveRepoPaths">
        /// When a subfolder is given, keep the full repo-relative layout inside the zip instead of making the
        /// subfolder the zip root. Ignored when archiving the whole repo.
        /// </param>
        public void Archive(string outputZipFile, string? subfolder = null, bool preserveRepoPaths = false)
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputZipFile))
            {
                Output.WriteLine(Color.Red, "Output zip file is required.");
                return;
            }

            if (!TryResolveSubfolder(subfolder, out string subfolderPrefix))
                return;

            string fullZipPath = Path.GetFullPath(outputZipFile);
            if (Directory.Exists(fullZipPath))
            {
                Output.WriteLine(Color.Red, "Output zip path points to a folder, not a file.");
                return;
            }

            // Create destination folder path
            string? zipDirectory = Path.GetDirectoryName(fullZipPath);
            if (!string.IsNullOrEmpty(zipDirectory))
                Directory.CreateDirectory(zipDirectory);

            if (File.Exists(fullZipPath))
            {
                Output.WriteLine(Color.Red, "Output zip file already exists.");
                return;
            }

            Changelist changes = GetChanges();
            if (HasUncommittedChanges(changes))
                Output.WriteLine(Color.Yellow, "Warning: repo has uncommitted changes. Archive will copy current tracked file contents; new untracked files are omitted.");

            if (!TrySelectPackFiles(subfolderPrefix, "archive", out List<string> tracked))
                return;

            using (ZipArchive archive = ZipFile.Open(fullZipPath, ZipArchiveMode.Create))
            {
                foreach (string relativePath in tracked)
                {
                    string sourcePath = Path.Combine(RootPath, relativePath);
                    archive.CreateEntryFromFile(sourcePath, NormalizeArchivePath(MapPackPath(relativePath, subfolderPrefix, preserveRepoPaths)), CompressionLevel.Optimal);
                    Output.WriteLine(Color.Green, $"Archived {relativePath}");
                }
            }

            Output.WriteLine(Color.GreenYellow, $"Archive created: {fullZipPath} ({tracked.Count} {(tracked.Count == 1 ? "file" : "files")})");
        }
        /// <summary>
        /// Create a restorable checkpoint archive containing the version history and all currently tracked files.
        /// </summary>
        /// <remarks>
        /// Deliberately whole-repo only: the checkpoint carries `.cv/versions`, whose records are repo-root
        /// relative, so a partial checkpoint would restore into a repo that immediately reports every excluded
        /// file as deleted. Use <see cref="Archive"/> when you only want a subfolder's contents.
        /// </remarks>
        public void CreateCheckpoint(string targetZipFile)
        {
            if (!RepoExists)
            {
                Output.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetZipFile))
            {
                Output.WriteLine(Color.Red, "Target zip file is required.");
                return;
            }

            Changelist changes = GetChanges();
            if (HasUncommittedChanges(changes))
            {
                Output.WriteLine(Color.Red, "Cannot create checkpoint because the repo has uncommitted changes.");
                return;
            }

            string fullZipPath = Path.GetFullPath(targetZipFile);
            if (Directory.Exists(fullZipPath))
            {
                Output.WriteLine(Color.Red, "Target zip path points to a folder, not a file.");
                return;
            }

            string? zipDirectory = Path.GetDirectoryName(fullZipPath);
            if (!string.IsNullOrEmpty(zipDirectory))
                Directory.CreateDirectory(zipDirectory);

            if (File.Exists(fullZipPath))
            {
                Output.WriteLine(Color.Red, "Target zip file already exists.");
                return;
            }

            RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
            List<string> tracked = storage
                .GetLatestFiles()
                .Keys
                .OrderBy(p => p)
                .ToList();

            List<string> missingFiles = tracked
                .Where(p => !File.Exists(Path.Combine(RootPath, p)))
                .ToList();

            if (missingFiles.Count > 0)
            {
                Output.WriteLine(Color.Red, "Cannot create checkpoint because some tracked files are missing:");
                foreach (string missing in missingFiles)
                    Output.WriteLine(Color.Yellow, missing);
                return;
            }

            using ZipArchive archive = ZipFile.Open(fullZipPath, ZipArchiveMode.Create);

            archive.CreateEntryFromFile(ChangelogFullFilePath, ChangelogArchivePath, CompressionLevel.Optimal);
            Output.WriteLine(Color.Green, $"Checkpointed {ChangelogArchivePath}");

            foreach (string relativePath in tracked)
            {
                string sourcePath = Path.Combine(RootPath, relativePath);
                archive.CreateEntryFromFile(sourcePath, NormalizeArchivePath(relativePath), CompressionLevel.Optimal);
                Output.WriteLine(Color.Green, $"Checkpointed {relativePath}");
            }

            Output.WriteLine(Color.GreenYellow, $"Checkpoint created: {fullZipPath}");
        }
        /// <summary>
        /// Restore a checkpoint archive into a clean folder.
        /// </summary>
        public void RestoreCheckpoint(string sourceZipFile)
        {
            if (RepoExists)
            {
                Output.WriteLine(Color.Red, "Cannot restore checkpoint because a CV repo already exists at this location.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceZipFile))
            {
                Output.WriteLine(Color.Red, "Source zip file is required.");
                return;
            }

            string fullZipPath = Path.GetFullPath(sourceZipFile);
            if (!File.Exists(fullZipPath))
            {
                Output.WriteLine(Color.Red, "Source zip file does not exist.");
                return;
            }

            string rootFullPath = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!IsCleanRestoreFolder(rootFullPath, fullZipPath))
            {
                Output.WriteLine(Color.Red, "Current folder must be empty before restoring a checkpoint, except for the checkpoint file itself.");
                return;
            }

            using ZipArchive archive = ZipFile.OpenRead(fullZipPath);

            bool hasHistory = archive.Entries.Any(e => NormalizeArchivePath(e.FullName) == ChangelogArchivePath);
            if (!hasHistory)
            {
                Output.WriteLine(Color.Red, $"Invalid checkpoint: missing {ChangelogArchivePath}.");
                return;
            }

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryName = NormalizeArchivePath(entry.FullName);
                if (string.IsNullOrWhiteSpace(entryName))
                    continue;

                string destinationPath;
                try
                {
                    destinationPath = GetSafeExtractionPath(rootFullPath, entryName);
                }
                catch (InvalidOperationException ex)
                {
                    Output.WriteLine(Color.Red, ex.Message);
                    return;
                }

                if (entryName.EndsWith("/", StringComparison.Ordinal))
                {
                    if (File.Exists(destinationPath))
                    {
                        Output.WriteLine(Color.Red, $"Cannot restore because a file already exists where a folder is needed: {entryName}");
                        return;
                    }

                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    Output.WriteLine(Color.Red, $"Cannot restore because destination already exists: {entryName}");
                    return;
                }
            }

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryName = NormalizeArchivePath(entry.FullName);
                if (string.IsNullOrWhiteSpace(entryName))
                    continue;

                string destinationPath = GetSafeExtractionPath(rootFullPath, entryName);

                if (entryName.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                entry.ExtractToFile(destinationPath, overwrite: false);
                Output.WriteLine(Color.Green, $"Restored {entryName}");
            }

            RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
            RestoreTrackedFileTimes(storage);

            int trackedCount = storage.GetLatestFiles().Count;
            Output.WriteLine(Color.GreenYellow, $"Checkpoint restored: {trackedCount} {(trackedCount == 1 ? "file" : "files")} tracked.");
        }
        #endregion

        #region Remote Sync
        /// <summary>
        /// Push new & updated files to a remote CheckVersion‐server.
        /// </summary>
        public async Task PushAsync(string serverUrl, string apiKey)
        {
            using HttpClient client = new() { BaseAddress = new Uri(serverUrl) };
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            Changelist changes = GetChanges();
            List<FileChangeRecord> toUpload = changes.NewFiles
                .Concat(changes.UpdatedFiles)
                .ToList();

            if (!toUpload.Any())
            {
                Output.WriteLine(Color.Yellow, "Nothing to push: working tree clean.");
                return;
            }

            foreach (FileChangeRecord? change in toUpload)
            {
                string local = Path.Combine(RootPath, change.Path);
                await using FileStream fs = File.OpenRead(local);
                StreamContent content = new(fs);

                // Escape spaces, special chars in URL
                string urlPath = "/files/" + Uri.EscapeDataString(change.Path);
                HttpResponseMessage resp = await client.PutAsync(urlPath, content);
                resp.EnsureSuccessStatusCode();

                Output.WriteLine(Color.Green, $"Pushed {change.Path}");
            }
        }
        /// <summary>
        /// Pull all files from remote CheckVersion‐server, overwriting local copies.
        /// </summary>
        public async Task PullAsync(string serverUrl, string apiKey)
        {
            using HttpClient client = new() { BaseAddress = new Uri(serverUrl) };
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            // Get list
            List<string>? files = await client.GetFromJsonAsync<List<string>>("/files");
            if (files == null || files.Count == 0)
            {
                Output.WriteLine(Color.Yellow, "No files on server.");
                return;
            }

            // Download each
            foreach (string? path in files.OrderBy(p => p))
            {
                string urlPath = "/files/" + Uri.EscapeDataString(path);
                HttpResponseMessage resp = await client.GetAsync(urlPath);
                if (!resp.IsSuccessStatusCode)
                {
                    Output.WriteLine(Color.Red, $"Failed to download {path}: {resp.StatusCode}");
                    continue;
                }

                string local = Path.Combine(RootPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(local)!);
                await using FileStream fs = File.Create(local);
                await resp.Content.CopyToAsync(fs);

                Output.WriteLine(Color.Green, $"Pulled {path}");
            }
        }
        #endregion

        #region Packing Helpers
        /// <summary>
        /// Validate an optional pack scope and turn it into a repo-relative prefix ("" for the whole repo).
        /// </summary>
        private bool TryResolveSubfolder(string? subfolder, out string prefix)
        {
            prefix = string.Empty;

            if (string.IsNullOrWhiteSpace(subfolder))
                return true;

            string candidate = subfolder.Trim();
            if (candidate == "." || candidate == "./")
                return true;

            string rootFullPath = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string subfolderFullPath = Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(rootFullPath, candidate))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(subfolderFullPath, rootFullPath, StringComparison.OrdinalIgnoreCase))
                return true;

            string rootWithSeparator = rootFullPath + Path.DirectorySeparatorChar;
            if (!subfolderFullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                Output.WriteLine(Color.Red, $"Subfolder must be inside the repo: {subfolder}");
                return false;
            }

            string relative = subfolderFullPath[rootWithSeparator.Length..].Replace('\\', '/').Trim('/');

            string controlFolder = NormalizeArchivePath(RepoControlFolderName).TrimEnd('/');
            if (controlFolder.Length > 0
                && (string.Equals(relative, controlFolder, StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith(controlFolder + "/", StringComparison.OrdinalIgnoreCase)))
            {
                Output.WriteLine(Color.Red, $"Subfolder must not be inside the repo control folder: {subfolder}");
                return false;
            }

            prefix = relative;
            return true;
        }
        /// <summary>
        /// Pick the tracked files in scope and verify they all exist on disk.
        /// </summary>
        private bool TrySelectPackFiles(string subfolderPrefix, string operationName, out List<string> selected)
        {
            RepoHistory storage = SerializationHelper.DeserializeFromFile(ChangelogFullFilePath);
            selected = storage
                .GetLatestFiles()
                .Keys
                .Where(p => IsUnderPrefix(p, subfolderPrefix))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // An empty whole-repo pack stays a no-op success (a fresh repo has nothing to pack yet), but an
            // empty subfolder pack almost always means a mistyped scope, so it is worth failing on.
            if (selected.Count == 0 && subfolderPrefix.Length > 0)
            {
                Output.WriteLine(Color.Red, $"Nothing to {operationName}: no tracked files under '{subfolderPrefix}'.");
                return false;
            }

            // Only the selected files matter, so an unrelated missing file elsewhere in a large repo
            // should not block packing a subfolder.
            List<string> missingFiles = selected
                .Where(p => !File.Exists(Path.Combine(RootPath, p)))
                .ToList();

            if (missingFiles.Count > 0)
            {
                Output.WriteLine(Color.Red, $"Cannot {operationName} because some tracked files are missing:");
                foreach (string missing in missingFiles)
                    Output.WriteLine(Color.Yellow, missing);
                return false;
            }

            return true;
        }
        private static bool IsUnderPrefix(string relativePath, string prefix)
            => prefix.Length == 0
            || (relativePath.Length > prefix.Length
                && relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && relativePath[prefix.Length] == '/');
        /// <summary>
        /// Decide where a tracked file lands inside the output. With a subfolder scope the subfolder becomes
        /// the output root by default, since that is what "just dump this small folder" usually means.
        /// </summary>
        private static string MapPackPath(string relativePath, string prefix, bool preserveRepoPaths)
            => prefix.Length == 0 || preserveRepoPaths
            ? relativePath
            : relativePath[(prefix.Length + 1)..];
        #endregion

        #region Helpers
        private Changelist GetChanges()
            => GetChanges(GetHistory());
        private Changelist GetChanges(RepoHistory storage)
        {
            if (!RepoExists)
                throw new InvalidOperationException("Must be inside a CV repo.");

            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> latest = storage.GetLatestFiles();
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime, long Size)> actual = GetActualFiles();
            DateTime lastCommit = storage.Commits.Count > 0 ? storage.Commits.Last().Time : DateTime.MinValue;

            Changelist changes = new();
            foreach ((string relativePath, (DateTime updateTime, DateTime creationTime, long size)) in actual)
            {
                // New files
                if (!latest.ContainsKey(relativePath))
                {
                    // Moved files
                    if (creationTime < lastCommit && latest.Any(f => f.Value.CreationTime == creationTime))
                    {
                        string movedFile = latest.First(f => f.Value.CreationTime == creationTime).Key;

                        changes.MovedFiles.Add(new FileChangeRecord()
                        {
                            ChangeType = FileChangeRecord.FileChangeType.Moved,
                            NewPath = relativePath,
                            Path = movedFile,
                            UpdateTime = updateTime,
                            Size = size
                        });

                        latest.Remove(movedFile);
                    }
                    else
                        changes.NewFiles.Add(new FileChangeRecord()
                        {
                            ChangeType = FileChangeRecord.FileChangeType.New,
                            NewPath = creationTime.Ticks.ToString(),
                            Path = relativePath,
                            UpdateTime = updateTime,
                            Size = size
                        });
                }
                // Updated files
                else
                {
                    if (updateTime > latest[relativePath].UpdateTime)
                    {
                        // Deleted then recreated file
                        if (latest[relativePath].CreationTime != creationTime)
                        {
                            changes.DeletedFiles.Add(new FileChangeRecord()
                            {
                                ChangeType = FileChangeRecord.FileChangeType.Deleted,
                                NewPath = null,
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = 0
                            });
                            changes.NewFiles.Add(new FileChangeRecord()
                            {
                                ChangeType = FileChangeRecord.FileChangeType.Recreated,
                                NewPath = creationTime.Ticks.ToString(),
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = size
                            });
                        }
                        else
                            changes.UpdatedFiles.Add(new FileChangeRecord()
                            {
                                ChangeType = FileChangeRecord.FileChangeType.Updated,
                                NewPath = null,
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = size
                            });
                    }

                    latest.Remove(relativePath);
                }
            }
            // Deleted files
            foreach (KeyValuePair<string, (DateTime UpdateTime, DateTime CreationTime)> item in latest)
                changes.DeletedFiles.Add(new FileChangeRecord()
                {
                    ChangeType = FileChangeRecord.FileChangeType.Deleted,
                    NewPath = null,
                    Path = item.Key,
                    UpdateTime = storage.Commits.Count > 0 ? storage.Commits.Last().Time : DateTime.Now.ToUniversalTime(),
                    Size = 0
                });

            return changes;
        }
        /// <summary>
        /// Get all the files that we recognize that's currently available for version tracking
        /// </summary>
        private Dictionary<string, (DateTime UpdateTime, DateTime CreationTime, long Size)> GetActualFiles()
        {
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime, long Size)> entries = [];
            IgnoreContext rootContext = IgnoreContext.FromRootRules(ReadIgnoreRules());

            string rootFullPath = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string controlFolderFullPath = RepoControlFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string ignoreFileName = IgnoreFileNameOnly;

            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
                ReturnSpecialDirectories = false
            };

            EnumerateAndAddFileEntry(new DirectoryInfo(rootFullPath), string.Empty, rootContext);
            return entries;

            void EnumerateAndAddFileEntry(DirectoryInfo currentFolder, string relativeFolderPath, IgnoreContext context)
            {
                // A `.cvignore` inside a subfolder layers on top of the rules inherited from above, with its
                // patterns interpreted relative to this folder. The root file is already in the context.
                if (relativeFolderPath.Length > 0 && !string.IsNullOrEmpty(ignoreFileName))
                {
                    string localIgnoreFile = Path.Combine(currentFolder.FullName, ignoreFileName);
                    if (File.Exists(localIgnoreFile))
                    {
                        List<IgnoreRule> localRules = ParseIgnoreRules(File.ReadAllLines(localIgnoreFile));
                        if (localRules.Count > 0)
                            context = context.Push(new IgnoreScope(relativeFolderPath, localRules));
                    }
                }

                // Recurse into subfolders unless ignored
                foreach (DirectoryInfo subFolder in currentFolder.EnumerateDirectories("*", options))
                {
                    string subFolderFullPath = subFolder.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Skip control folder
                    if (string.Equals(subFolderFullPath, controlFolderFullPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Compute the relative path for matching
                    string relativeFolder = Path.GetRelativePath(rootFullPath, subFolder.FullName).Replace('\\', '/');

                    // If the ignore rules say to ignore this directory, don't even recurse into it
                    if (context.ShouldIgnore(relativeFolder))
                        continue;

                    EnumerateAndAddFileEntry(subFolder, relativeFolder, context);
                }
                // Enumerate files in non‐ignored folders
                foreach (FileInfo file in currentFolder.EnumerateFiles("*", options))
                {
                    string relativePath = Path.GetRelativePath(rootFullPath, file.FullName).Replace('\\', '/');
                    if (!context.ShouldIgnore(relativePath))
                        entries[relativePath] = (file.LastWriteTimeUtc, file.CreationTimeUtc, file.Length);
                }
            }
        }
        /// <summary>
        /// Read the repo-root ignore file. Nested ignore files are discovered during the folder walk.
        /// </summary>
        public List<IgnoreRule> ReadIgnoreRules()
            => File.Exists(IgnoreFullFilePath)
            ? ParseIgnoreRules(File.ReadAllLines(IgnoreFullFilePath))
            : [];
        /// <summary>
        /// Parse ignore file lines, skipping blanks and comments.
        /// </summary>
        public static List<IgnoreRule> ParseIgnoreRules(IEnumerable<string> lines)
            => lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#"))
            .Select(line => new IgnoreRule(line))
            .ToList();
        public static bool ShouldIgnore(IEnumerable<IgnoreRule> rules, string path, string repoRoot = "")
        {
            // Normalize to forward‐slashes
            path = path.Replace('\\', '/').TrimStart('/');
            bool? ignored = null;

            foreach (IgnoreRule rule in rules)
            {
                if (!rule.IsMatch(path, repoRoot))
                    continue;

                // Last matching rule wins
                ignored = !rule.IsNegation;
            }

            return ignored.GetValueOrDefault(false);
        }
        private static bool HasUncommittedChanges(Changelist changes)
            => changes.NewFiles.Any() ||
               changes.UpdatedFiles.Any() ||
               changes.MovedFiles.Any() ||
               changes.DeletedFiles.Any();
        private static string NormalizeArchivePath(string path)
            => path.Replace('\\', '/').TrimStart('/');
        private static bool IsCleanRestoreFolder(string rootFullPath, string sourceZipFullPath)
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(rootFullPath))
            {
                string entryFullPath = Path.GetFullPath(entry).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string sourceFullPath = sourceZipFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (string.Equals(entryFullPath, sourceFullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                return false;
            }

            return true;
        }
        private void RestoreTrackedFileTimes(RepoHistory storage)
        {
            foreach ((string relativePath, (DateTime updateTime, DateTime creationTime)) in storage.GetLatestFiles())
            {
                string fullPath = Path.Combine(RootPath, relativePath);
                if (!File.Exists(fullPath))
                    continue;

                File.SetCreationTimeUtc(fullPath, creationTime);
                File.SetLastWriteTimeUtc(fullPath, updateTime);
            }
        }
        private static string GetSafeExtractionPath(string rootFullPath, string entryName)
        {
            string normalizedName = entryName.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            string destinationPath = Path.GetFullPath(Path.Combine(rootFullPath, normalizedName));
            string rootWithSeparator = rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!destinationPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsafe checkpoint entry path: {entryName}");

            return destinationPath;
        }
        #endregion
    }
}
