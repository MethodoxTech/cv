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
            Moved   // Save file save name save creation date somewhere else
        }

        public FileChangeType ChangeType;
        public string Path; // All paths are relative
        public string NewPath;
        public DateTime UpdateTime;
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
        }

        public List<Commit> Commits = new List<Commit>();

        #region Helper Accessor
        public Dictionary<string, DateTime> GetLatestFiles()
        {
            Dictionary<string, DateTime> files = new Dictionary<string, DateTime>();
            foreach (var commit in Commits)
            {
                foreach (var fileChange in commit.Changes)
                {
                    switch (fileChange.ChangeType)
                    {
                        case FileChange.FileChangeType.New:
                        case FileChange.FileChangeType.Updated:
                        case FileChange.FileChangeType.Deleted:
                            files[fileChange.Path] = fileChange.UpdateTime;
                            break;
                        case FileChange.FileChangeType.Moved:
                            files.Remove(fileChange.Path);
                            files[fileChange.NewPath] = fileChange.UpdateTime;
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
                Console.WriteLine($"Usage: cv status|init|commit -m <Message>", Color.DarkGreen);
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
                        Commit();
                        break;
                    default:
                        break;
                }
            }
        }

        #region Routines
        private static void Commit()
        {
            if (!Directory.Exists(RepoControlFolderName))
                Console.WriteLine("No repo exists at current location", Color.DarkRed);
            else
            {
                var changes = GetChanges();

                var storage = new YamlDotNet.Serialization.Deserializer().Deserialize<RepoStorage>(File.ReadAllText(RepoStorageFilePath));
                storage.Commits.Add(new RepoStorage.Commit()
                {
                    Changes = changes.NewFiles
                    .Union(changes.UpdatedFiles)
                    .Union(changes.MovedFiles)
                    .Union(changes.DeletedFiles)
                    .ToList()
                });
                File.WriteAllText(RepoStorageFilePath, new YamlDotNet.Serialization.Serializer().Serialize(storage));
            }
        }

        private static void Init()
        {
            if (Directory.Exists(RepoControlFolderName))
                Console.WriteLine("A CV repo already exists at this location.", Color.DarkRed);
            else
            {
                Directory.CreateDirectory(RepoControlFolderName);
                File.WriteAllText(RepoStorageFilePath, new YamlDotNet.Serialization.Serializer().Serialize(new RepoStorage()));
                Console.WriteLine($"Repo initialized at: {RepoRootPath}", Color.GreenYellow);
            }
        }

        private static void Status()
        {
            var changes = GetChanges();
            Console.WriteLine($"New: {changes.NewFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.NewFiles)
            {
                Console.Write($"{file.Path} ", Color.Green);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"Updated: {changes.UpdatedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.UpdatedFiles)
            {
                Console.Write($"{file.Path} ", Color.Green);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"Moved: {changes.MovedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.MovedFiles)
            {
                Console.Write($"{file.Path} ", Color.Green);
                Console.WriteLine(file.UpdateTime.ToLocalTime(), Color.DarkGray);
            }

            Console.WriteLine($"Deleted: {changes.DeletedFiles.Count}", Color.Goldenrod);
            foreach (var file in changes.DeletedFiles)
            {
                Console.Write($"{file.Path} ", Color.Green);
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

            Changelist changes = new Changelist();
            foreach ((string relativePath, DateTime updateTime) in actual)
            {
                if (!latest.ContainsKey(relativePath))
                    changes.NewFiles.Add(new FileChange()
                    {
                        ChangeType = FileChange.FileChangeType.New,
                        NewPath = null,
                        Path = relativePath,
                        UpdateTime = updateTime
                    });
            }
            return changes;
        }
        private static Dictionary<string, DateTime> GetActualFiles()
        {
            string[] ignoreRules = null;
            if (File.Exists(IgnoreFilename))
                ignoreRules = File.ReadAllLines(IgnoreFilename);

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
                    if (!ShouldIgnore(ignoreRules, relativePath))
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
        private const string IgnoreFilename = ".gitignore";
        private static string RepoStorageFilePath = Path.Combine(RepoControlFolderName, "versions");
    }
}