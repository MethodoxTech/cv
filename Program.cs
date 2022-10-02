using System.Drawing;
using Console = Colorful.Console;
namespace cv
{
    public class FileChange
    {
        public enum FileChangeType
        {
            New,    // Completely new
            Updated, // Same path, different update time
            Deleted, // No longer exist at path
            Moved,   // Save file save name save creation date somewhere else
            Recreated, // Deleted then recreated with the same path
        }

        public FileChangeType ChangeType;
        public string Path; // All paths are relative
        public string NewPath;
        public DateTime UpdateTime;
        public long Size;
    }

    public class Changelist
    {
        public List<FileChange> NewFiles = new List<FileChange>();
        public List<FileChange> UpdatedFiles = new List<FileChange>();
        public List<FileChange> DeletedFiles = new List<FileChange>();
        public List<FileChange> MovedFiles = new List<FileChange>();
    }
    public class RepoStorage
    {
        public class Commit
        {
            public List<FileChange> Changes = new ();
            public string Message;
            public DateTime Time;
        }

        public List<Commit> Commits = new List<Commit>();

        #region Helper Accessor
        public Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> GetLatestFiles()
        {
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> files = new ();
            foreach (var commit in Commits)
            {
                foreach (var fileChange in commit.Changes)
                {
                    switch (fileChange.ChangeType)
                    {
                        case FileChange.FileChangeType.New:
                        case FileChange.FileChangeType.Recreated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: new DateTime(long.Parse(fileChange.NewPath)));
                            break;
                        case FileChange.FileChangeType.Updated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: files[fileChange.Path].CreationTime);
                            break;
                        case FileChange.FileChangeType.Deleted:
                            files.Remove(fileChange.Path);
                            break;
                        case FileChange.FileChangeType.Moved:
                            files.Remove(fileChange.Path);
                            files[fileChange.NewPath] = (UpdateTime: fileChange.UpdateTime, CreationTime: files[fileChange.Path].CreationTime);
                            break;
                        default:
                            break;
                    }
                }
            }
            return files;
        }
        #endregion
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            string directory = RepoRootPath;
            if (args.Length == 0)
            {
                Console.WriteLine($"Usage: cv status|init|commit -m <Message>|log", Color.DarkGreen);
            }
            else
            {
                string action = args[0].ToLower();
                switch (action)
                {
                    case "status":
                        Status();
                        break;
                    case "init":
                        Init();
                        break;
                    case "commit":
                        if (args.Length != 3)
                            Console.WriteLine("commit -m <Message>", Color.Red);
                        else
                            Commit(args[2]);
                        break;
                    case "log":
                        Log();
                        break;
                    default:
                        break;
                }
            }
        }
        #region Routines
        private static void Log()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine("No repo exists at current location", Color.Red);
                return;
            }

            var storage = new YamlDotNet.Serialization.Deserializer().Deserialize<RepoStorage>(File.ReadAllText(RepoStorageFilePath));
            for (int i = 0; i < storage.Commits.Count; i++)
            {
                RepoStorage.Commit commit = storage.Commits[i];
                Console.Write($"{i}.".PadRight(3));
                Console.Write(commit.Time.ToLocalTime().ToString() + " ", Color.Green);
                Console.WriteLine(commit.Message, Color.White);
            }
            Console.WriteLine($"{storage.Commits.Count} {(storage.Commits.Count <= 1 ? "commit": "commits")}.", Color.Goldenrod);
        }
        private static void Commit(string message)
        {
            if (!Directory.Exists(RepoControlFolderName))
                Console.WriteLine("No repo exists at current location", Color.Red);
            else
            {
                var changes = GetChanges();

                var storage = new YamlDotNet.Serialization.Deserializer().Deserialize<RepoStorage>(File.ReadAllText(RepoStorageFilePath));
                var allChanges = changes.DeletedFiles
                    .Union(changes.UpdatedFiles)
                    .Union(changes.MovedFiles)
                    .Union(changes.NewFiles) // Order matters, we must union DeletedFiles first because in the case of FileChangeType.Recreate, we want to maintain that relation
                    .ToList();

                if (allChanges.Count == 0)
                {
                    Console.WriteLine("There is no changed file, are you sure you want to make an empty commit? [Y/N]", Color.Red);
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
                File.WriteAllText(RepoStorageFilePath, new YamlDotNet.Serialization.Serializer().Serialize(storage));
                Console.WriteLine($"Saved {allChanges.Count} {(allChanges.Count <= 1 ? "file" : "files")}.", Color.Goldenrod);
            }
        }

        private static void Init()
        {
            if (Directory.Exists(RepoControlFolderName))
                Console.WriteLine("A CV repo already exists at this location.", Color.Red);
            else
            {
                Directory.CreateDirectory(RepoControlFolderName);
                File.WriteAllText(RepoStorageFilePath, new YamlDotNet.Serialization.Serializer().Serialize(new RepoStorage()));
                Console.WriteLine($"Repo initialized at: {RepoRootPath}", Color.GreenYellow);
            }
        }

        private static void Status()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine("No repo exists at current location", Color.Red);
                return;
            }

            var changes = GetChanges();
            Console.WriteLine($"# New: {changes.NewFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.NewFiles)
            {
                Console.Write($"{file.Path} ", Color.Green);
                if (file.ChangeType == FileChange.FileChangeType.Recreated)
                {
                    Console.Write(file.UpdateTime.ToLocalTime(), Color.DarkGray);
                    Console.WriteLine(" [Recreated]", Color.Yellow);
                }
                else 
                    Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"# Updated: {changes.UpdatedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.UpdatedFiles)
            {
                Console.Write($"{file.Path} ", Color.YellowGreen);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"# Moved: {changes.MovedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.MovedFiles)
            {
                Console.Write($"{file.Path} ", Color.SkyBlue);
                Console.Write($"-> ", Color.Yellow);
                Console.Write($"{file.NewPath} ", Color.SkyBlue);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"# Deleted: {changes.DeletedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.DeletedFiles)
            {
                Console.Write($"{file.Path} ", Color.DarkRed);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

        }
        #endregion

        #region Helpers
        private static Changelist GetChanges()
        {
            if (!Directory.Exists(RepoControlFolderName))
                throw new InvalidOperationException("Must be inside a CV repo.");

            var storage = new YamlDotNet.Serialization.Deserializer().Deserialize<RepoStorage>(File.ReadAllText(RepoStorageFilePath));
            var latest = storage.GetLatestFiles();
            var actual = GetActualFiles();
            var lastCommit = storage.Commits.Count > 0 ? storage.Commits.Last().Time : DateTime.MinValue;

            Changelist changes = new Changelist();
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
            foreach (var item in latest)
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

        private static Dictionary<string, DateTime> GetActualFiles()
        {
            string[] ignoreRules = null;
            if (File.Exists(IgnoreFilename))
                ignoreRules = File.ReadAllLines(IgnoreFilename).Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith('#')).ToArray();

            Dictionary<string, DateTime> entries = new Dictionary<string, DateTime>();
            EnumerateAndAddFileEntry(RepoRootPath);
            return entries;

            void EnumerateAndAddFileEntry(string currentFolder)
            {
                foreach (var subFolder in Directory.EnumerateDirectories(currentFolder))
                {
                    if (currentFolder == RepoRootPath && Path.GetFileName(subFolder) == RepoControlFolderName)
                        continue;
                    else
                        EnumerateAndAddFileEntry(subFolder);
                }
                foreach (var file in Directory.EnumerateFiles(currentFolder))
                {
                    string relativePath = Path.GetRelativePath(RepoRootPath, file).Replace('\\', '/');
                    if (ignoreRules == null || !ShouldIgnore(ignoreRules, relativePath))
                        entries[relativePath] = File.GetLastWriteTimeUtc(file);
                }
            }
        }
        private static bool ShouldIgnore(string[] rules, string path)
        {
            return rules.Any(r => path.StartsWith(r));
        }
        #endregion

        private static string RepoRootPath = Directory.GetCurrentDirectory();
        private const string RepoControlFolderName = ".cv";
        private const string IgnoreFilename = ".cvignore";
        private static string RepoStorageFilePath = Path.Combine(RepoControlFolderName, "versions");
    }
}