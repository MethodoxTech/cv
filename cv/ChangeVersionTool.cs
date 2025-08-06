using cv.Serialization;
using cv.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Color = System.Drawing.Color;
using Console = cv.Types.ColorConsole;

namespace cv
{
    public class ChangeVersionTool
    {
        #region Properties
        public string RootPath { get; }
        public string RepoControlFolderName { get; }
        public string StorageFilePath { get; }
        public string IgnoreFilename { get; }
        public ChangeVersionTool(string repoRootPath, string repoControlFolderName, string repoStorageFilePath, string ignoreFilename)
        {
            RootPath = repoRootPath;
            RepoControlFolderName = repoControlFolderName;
            StorageFilePath = repoStorageFilePath;
            IgnoreFilename = ignoreFilename;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Log all existing commits.
        /// </summary>
        public void Log()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            RepoStorage storage = SerializationHelper.DeserializeFromFile(StorageFilePath);
            for (int i = 0; i < storage.Commits.Count; i++)
            {
                RepoStorage.Commit commit = storage.Commits[i];
                Console.Write($"{i}.".PadRight(3));
                Console.Write(Color.Green, commit.Time.ToLocalTime().ToString() + " ");
                Console.WriteLine(Color.White, commit.Message);
            }
            Console.WriteLine(Color.Goldenrod, $"{storage.Commits.Count} {(storage.Commits.Count <= 1 ? "commit" : "commits")}.");
        }
        /// <summary>
        /// Commit current changes to the repo.
        /// </summary>
        public void Commit(string message)
        {
            if (!Directory.Exists(RepoControlFolderName))
                Console.WriteLine(Color.Red, "No repo exists at current location");
            else
            {
                Changelist changes = GetChanges();

                RepoStorage storage = SerializationHelper.DeserializeFromFile(StorageFilePath);
                List<FileChange> allChanges = changes.DeletedFiles
                    .Union(changes.UpdatedFiles)
                    .Union(changes.MovedFiles)
                    .Union(changes.NewFiles) // Order matters, we must union DeletedFiles first because in the case of FileChangeType.Recreate, we want to maintain that relation
                    .ToList();

                if (allChanges.Count == 0)
                {
                    Console.WriteLine(Color.Red, "There is no changed file, are you sure you want to make an empty commit? [Y/N]");
                    string input = Console.ReadLine().Trim().ToLower();
                    if (input == "n" || input == "no" || input == "f")
                        return;
                }
                storage.Commits.Add(new RepoStorage.Commit()
                {
                    Changes = allChanges,
                    Message = message,
                    Time = DateTime.Now.ToUniversalTime()
                });
                SerializationHelper.SerializeToFile(storage, StorageFilePath);
                Console.WriteLine(Color.Goldenrod, $"Saved {allChanges.Count} {(allChanges.Count <= 1 ? "file" : "files")}.");
            }
        }
        /// <summary>
        /// Initialize a new repo.
        /// </summary>
        public void Init()
        {
            if (Directory.Exists(RepoControlFolderName))
                Console.WriteLine(Color.Red, "A CV repo already exists at this location.");
            else
            {
                Directory.CreateDirectory(RepoControlFolderName);
                SerializationHelper.SerializeToFile(new RepoStorage(), StorageFilePath);
                Console.WriteLine(Color.GreenYellow, $"Repo initialized at: {RootPath}");
            }
        }
        /// <summary>
        /// Print all the changes.
        /// </summary>
        public void Status()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            Changelist changes = GetChanges();
            Console.WriteLine(Color.Goldenrod, $"# New: {changes.NewFiles.Count}");
            foreach (FileChange file in changes.NewFiles)
            {
                Console.Write(Color.Green, $"{file.Path} ");
                if (file.ChangeType == FileChange.FileChangeType.Recreated)
                {
                    Console.Write(Color.DarkGray, file.UpdateTime.ToLocalTime());
                    Console.WriteLine(Color.Yellow, " [Recreated]");
                }
                else
                    Console.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime());
            }

            Console.WriteLine(Color.Goldenrod, $"# Updated: {changes.UpdatedFiles.Count}");
            foreach (FileChange file in changes.UpdatedFiles)
            {
                Console.Write(Color.YellowGreen, $"{file.Path} ");
                Console.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime());
            }

            Console.WriteLine(Color.Goldenrod, $"# Moved: {changes.MovedFiles.Count}");
            foreach (FileChange file in changes.MovedFiles)
            {
                Console.Write(Color.SkyBlue, $"{file.Path} ");
                Console.Write(Color.Yellow, $"-> ");
                Console.Write(Color.SkyBlue, $"{file.NewPath} ");
                Console.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime());
            }

            Console.WriteLine(Color.Goldenrod, $"# Deleted: {changes.DeletedFiles.Count}");
            foreach (FileChange file in changes.DeletedFiles)
            {
                Console.Write(Color.DarkRed, $"{file.Path} ");
                Console.WriteLine(Color.DarkGray, file.UpdateTime.ToLocalTime());
            }
        }
        /// <summary>
        /// List all tracked files, and show any uncommitted changes.
        /// </summary>
        public void List()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            // Load tracked files
            RepoStorage storage = SerializationHelper.DeserializeFromFile(StorageFilePath);
            List<string> tracked = storage
                .GetLatestFiles()
                .Keys
                .OrderBy(p => p)
                .ToList();

            Console.WriteLine(Color.Cyan, "# Tracked files:");
            foreach (string? path in tracked)
                Console.WriteLine(Color.White, path);

            // Compute any pending changes
            Changelist changes = GetChanges();
            bool hasChanges =
                changes.NewFiles.Any() ||
                changes.UpdatedFiles.Any() ||
                changes.MovedFiles.Any() ||
                changes.DeletedFiles.Any();

            if (hasChanges)
            {
                Console.WriteLine();
                Console.WriteLine(Color.Goldenrod, "# Uncommitted changes:");

                foreach (FileChange f in changes.NewFiles)
                    Console.WriteLine(Color.Green, $"New:     {f.Path}");
                foreach (FileChange f in changes.UpdatedFiles)
                    Console.WriteLine(Color.YellowGreen, $"Updated: {f.Path}");
                foreach (FileChange f in changes.MovedFiles)
                    Console.WriteLine(Color.SkyBlue, $"Moved:   {f.Path} → {f.NewPath}");
                foreach (FileChange f in changes.DeletedFiles)
                    Console.WriteLine(Color.DarkRed, $"Deleted: {f.Path}");
            }
        }
        #endregion

        #region Routines

        #endregion

        #region Helpers
        private Changelist GetChanges()
        {
            if (!Directory.Exists(RepoControlFolderName))
                throw new InvalidOperationException("Must be inside a CV repo.");

            RepoStorage storage = SerializationHelper.DeserializeFromFile(StorageFilePath);
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> latest = storage.GetLatestFiles();
            Dictionary<string, DateTime> actual = GetActualFiles();
            DateTime lastCommit = storage.Commits.Count > 0 ? storage.Commits.Last().Time : DateTime.MinValue;

            Changelist changes = new();
            foreach ((string relativePath, DateTime updateTime) in actual)
            {
                // New files
                if (!latest.ContainsKey(relativePath))
                {
                    // Moved files
                    if (File.GetCreationTimeUtc(relativePath) < lastCommit
                        && latest.Any(f => f.Value.CreationTime == File.GetCreationTimeUtc(relativePath)))
                    {
                        string movedFile = latest.First(f => f.Value.CreationTime == File.GetCreationTimeUtc(relativePath)).Key;

                        changes.MovedFiles.Add(new FileChange()
                        {
                            ChangeType = FileChange.FileChangeType.Moved,
                            NewPath = relativePath,
                            Path = movedFile,
                            UpdateTime = updateTime,
                            Size = new FileInfo(relativePath).Length
                        });

                        latest.Remove(movedFile);
                    }
                    else
                        changes.NewFiles.Add(new FileChange()
                        {
                            ChangeType = FileChange.FileChangeType.New,
                            NewPath = File.GetCreationTimeUtc(relativePath).Ticks.ToString(),
                            Path = relativePath,
                            UpdateTime = updateTime,
                            Size = new FileInfo(relativePath).Length
                        });
                }
                // Updated files
                else
                {
                    if (updateTime > latest[relativePath].UpdateTime)
                    {
                        // Deleted then recreated file
                        if (latest[relativePath].CreationTime != File.GetCreationTimeUtc(relativePath))
                        {
                            changes.DeletedFiles.Add(new FileChange()
                            {
                                ChangeType = FileChange.FileChangeType.Deleted,
                                NewPath = null,
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = 0
                            });
                            changes.NewFiles.Add(new FileChange()
                            {
                                ChangeType = FileChange.FileChangeType.Recreated,
                                NewPath = File.GetCreationTimeUtc(relativePath).Ticks.ToString(),
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = new FileInfo(relativePath).Length
                            });
                        }
                        else
                            changes.UpdatedFiles.Add(new FileChange()
                            {
                                ChangeType = FileChange.FileChangeType.Updated,
                                NewPath = null,
                                Path = relativePath,
                                UpdateTime = updateTime,
                                Size = new FileInfo(relativePath).Length
                            });
                    }

                    latest.Remove(relativePath);
                }
            }
            // Deleted files
            foreach (KeyValuePair<string, (DateTime UpdateTime, DateTime CreationTime)> item in latest)
                changes.DeletedFiles.Add(new FileChange()
                {
                    ChangeType = FileChange.FileChangeType.Deleted,
                    NewPath = null,
                    Path = item.Key,
                    UpdateTime = storage.Commits.Count > 0 ? storage.Commits.Last().Time : DateTime.Now.ToUniversalTime(),
                    Size = 0
                });

            return changes;
        }
        /// <summary>
        /// Get all the files that we recognize that's currently under version tracking
        /// </summary>
        private Dictionary<string, DateTime> GetActualFiles()
        {
            List<IgnoreRule> ignoreRules = ReadIgnoreRules();

            Dictionary<string, DateTime> entries = [];
            EnumerateAndAddFileEntry(RootPath);
            return entries;

            void EnumerateAndAddFileEntry(string currentFolder)
            {
                foreach (string subFolder in Directory.EnumerateDirectories(currentFolder))
                {
                    if (currentFolder == RootPath && Path.GetFileName(subFolder) == RepoControlFolderName)
                        continue;
                    else
                        EnumerateAndAddFileEntry(subFolder);
                }
                foreach (string file in Directory.EnumerateFiles(currentFolder))
                {
                    string relativePath = Path.GetRelativePath(RootPath, file).Replace('\\', '/');
                    if (ignoreRules == null || !ShouldIgnore(ignoreRules, relativePath))
                        entries[relativePath] = File.GetLastWriteTimeUtc(file);
                }
            }
        }
        public List<IgnoreRule> ReadIgnoreRules()
        {
            if (!File.Exists(IgnoreFilename))
                return [];

            // Skip empty lines and lines with comments
            return File.ReadAllLines(IgnoreFilename)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#"))
                .Select(line => new IgnoreRule(line))
                .ToList();
        }
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
        #endregion
    }
}
