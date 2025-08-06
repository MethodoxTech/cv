using cv.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Color = System.Drawing.Color;
using Console = cv.Types.ColorConsole;

namespace cv
{
    [YamlStaticContext]
    [YamlSerializable(typeof(RepoStorage))]
    [YamlSerializable(typeof(RepoStorage.Commit))]
    [YamlSerializable(typeof(FileChange))]
    [YamlSerializable(typeof(Changelist))]
    [YamlSerializable(typeof(FileChange.FileChangeType))]
    public partial class YamlStaticContext : YamlDotNet.Serialization.StaticContext
    {
    }

    public static class SerializationHelper
    {
        #region Configurations
        private static IDeserializer _deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        private static ISerializer serializer = new StaticSerializerBuilder(new YamlStaticContext())
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .EnsureRoundtrip()
            .Build();
        #endregion

        #region Methods
        internal static RepoStorage DeserializeFromFile(string repoStorageFilePath)
            => _deserializer.Deserialize<RepoStorage>(File.ReadAllText(repoStorageFilePath));
        internal static void SerializeToFile(RepoStorage storage, string repoStorageFilePath)
            => File.WriteAllText(repoStorageFilePath, serializer.Serialize(storage));
        #endregion
    }

    internal class Program
    {
        #region Constants
        private static readonly string RepoRootPath = Directory.GetCurrentDirectory();
        private static string RepoStorageFilePath = Path.Combine(RepoControlFolderName, "versions");
        private const string RepoControlFolderName = ".cv";
        private const string IgnoreFilename = ".cvignore";
        #endregion

        #region Methods
        static void Main(string[] args)
        {
            // Print help
            if (args.Length == 0 ||
                args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                if (args[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine(Color.DarkGreen, $"Usage: cv status|init|commit|log -m <Message>|log");
                else
                    PrintDetailedHelp();
                return;
            }
            else if (args.Length == 0 ||
                args[0].Equals("version", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-v", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("--version", StringComparison.OrdinalIgnoreCase))
            {
                PrintVersion();
                return;
            }

            // Take action
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
                        Console.WriteLine(Color.Red, "commit -m <Message>");
                    else
                        Commit(args[2]);
                    break;
                case "log":
                    Log();
                    break;
                default:
                    Console.WriteLine($"Unrecognized command: {action}");
                    break;
            }
        }
        #endregion

        #region Routines
        private static void PrintDetailedHelp()
        {
            const string helpText = """
                cv — Change Version CLI

                Usage:
                  cv <command> [options]

                Commands:
                  init               Initialize a new cv repo in the current directory
                  status             Show uncommitted file changes (like `git status`)
                  commit -m <msg>    Commit current changes with message <msg>
                  log                Show commit history

                Options:
                  -h, --help, help   Show this help information

                See also `.cvignore` to exclude files from tracking.
                """;
            Console.WriteLine(Color.Cyan, helpText);
        }
        private static void PrintVersion()
            => Console.WriteLine("cv — Change Version CLI v1.0.3");
        /// <summary>
        /// Log all existing commits.
        /// </summary>
        private static void Log()
        {
            if (!Directory.Exists(RepoControlFolderName))
            {
                Console.WriteLine(Color.Red, "No repo exists at current location");
                return;
            }

            RepoStorage storage = SerializationHelper.DeserializeFromFile(RepoStorageFilePath);
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
        private static void Commit(string message)
        {
            if (!Directory.Exists(RepoControlFolderName))
                Console.WriteLine(Color.Red, "No repo exists at current location");
            else
            {
                Changelist changes = GetChanges();

                RepoStorage storage = SerializationHelper.DeserializeFromFile(RepoStorageFilePath);
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
                SerializationHelper.SerializeToFile(storage, RepoStorageFilePath);
                Console.WriteLine(Color.Goldenrod, $"Saved {allChanges.Count} {(allChanges.Count <= 1 ? "file" : "files")}.");
            }
        }
        /// <summary>
        /// Initialize a new repo.
        /// </summary>
        private static void Init()
        {
            if (Directory.Exists(RepoControlFolderName))
                Console.WriteLine(Color.Red, "A CV repo already exists at this location.");
            else
            {
                Directory.CreateDirectory(RepoControlFolderName);
                SerializationHelper.SerializeToFile(new RepoStorage(), RepoStorageFilePath);
                Console.WriteLine(Color.GreenYellow, $"Repo initialized at: {RepoRootPath}");
            }
        }
        /// <summary>
        /// Print all the changes.
        /// </summary>
        private static void Status()
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
        #endregion

        #region Helpers
        private static Changelist GetChanges()
        {
            if (!Directory.Exists(RepoControlFolderName))
                throw new InvalidOperationException("Must be inside a CV repo.");

            RepoStorage storage = SerializationHelper.DeserializeFromFile(RepoStorageFilePath);
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
        private static Dictionary<string, DateTime> GetActualFiles()
        {
            string[] ignoreRules = ReadIgnoreRules();

            Dictionary<string, DateTime> entries = [];
            EnumerateAndAddFileEntry(RepoRootPath);
            return entries;

            void EnumerateAndAddFileEntry(string currentFolder)
            {
                foreach (string subFolder in Directory.EnumerateDirectories(currentFolder))
                {
                    if (currentFolder == RepoRootPath && Path.GetFileName(subFolder) == RepoControlFolderName)
                        continue;
                    else
                        EnumerateAndAddFileEntry(subFolder);
                }
                foreach (string file in Directory.EnumerateFiles(currentFolder))
                {
                    string relativePath = Path.GetRelativePath(RepoRootPath, file).Replace('\\', '/');
                    if (ignoreRules == null || !ShouldIgnore(ignoreRules, relativePath))
                        entries[relativePath] = File.GetLastWriteTimeUtc(file);
                }
            }
        }
        private static string[]? ReadIgnoreRules()
        {
            string[]? ignoreRules = null;
            if (File.Exists(IgnoreFilename))
                ignoreRules = File.ReadAllLines(IgnoreFilename).Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith('#')).ToArray(); // Skip empty lines and lines with comments
            return ignoreRules;
        }
        private static bool ShouldIgnore(string[] rules, string path)
            => rules.Any(path.StartsWith);
        #endregion
    }
}