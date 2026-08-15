using CheckVersion.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using Console = CheckVersion.Types.ColorConsole;

namespace CheckVersion
{
    internal class Program
    {
        #region Constants
        internal const string Version = "1.2.0";
        private static readonly string RepoRootPath = Directory.GetCurrentDirectory();
        internal static readonly string RepoStorageFilePath = RepoDefaults.StorageFilePath;
        internal const string RepoControlFolderName = RepoDefaults.ControlFolderName;
        internal const string IgnoreFilename = RepoDefaults.IgnoreFilename;
        #endregion

        #region Methods
        private static async Task Main(string[] args)
        {
            // Print help
            if (args.Length == 0)
            {
                PrintDetailedHelp();
                return;
            }

            if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                if (args[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine(Color.DarkGreen, $"Usage: cv status|init|list|commit|log|gather|archive|checkpoint");
                else
                    PrintDetailedHelp();
                return;
            }
            else if (args[0].Equals("version", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-v", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("--version", StringComparison.OrdinalIgnoreCase))
            {
                PrintVersion();
                return;
            }

            CheckVersionTool tool = new(RepoRootPath, RepoControlFolderName, RepoStorageFilePath, IgnoreFilename);

            // Take action
            string action = args[0].ToLower();
            switch (action)
            {
                case "status":
                    tool.Status();
                    break;
                case "init":
                    tool.Init();
                    break;
                case "list":
                    tool.List();
                    break;
                case "commit":
                    if (args.Length != 3 || !args[1].Equals("-m", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine(Color.Red, "commit -m <Message>");
                    else
                        tool.Commit(args[2]);
                    break;
                case "log":
                    tool.Log();
                    break;
                case "gather":
                    RunPackCommand(args, "gather", "Usage: cv gather <output folder> [--subfolder <path>] [--full-paths]",
                        (target, subfolder, preserveRepoPaths) => tool.Gather(target, subfolder, preserveRepoPaths));
                    break;
                case "archive":
                    RunPackCommand(args, "archive", "Usage: cv archive <output zip file> [--subfolder <path>] [--full-paths]",
                        (target, subfolder, preserveRepoPaths) => tool.Archive(target, subfolder, preserveRepoPaths));
                    break;
                case "checkpoint":
                    if (args.Length != 3)
                        Console.WriteLine(Color.Red, "Usage: cv checkpoint create <target zip file>|restore <source zip file>");
                    else if (args[1].Equals("create", StringComparison.OrdinalIgnoreCase))
                        tool.CreateCheckpoint(args[2]);
                    else if (args[1].Equals("restore", StringComparison.OrdinalIgnoreCase))
                        tool.RestoreCheckpoint(args[2]);
                    else
                        Console.WriteLine(Color.Red, "Usage: cv checkpoint create <target zip file>|restore <source zip file>");
                    break;
                case "push":
                    if (args.Length != 3)
                        Console.WriteLine(Color.Red, "Usage: cv push <serverUrl> <apiKey>");
                    else
                        await tool.PushAsync(args[1], args[2]);
                    break;
                case "pull":
                    if (args.Length != 3)
                        Console.WriteLine(Color.Red, "Usage: cv pull <serverUrl> <apiKey>");
                    else
                        await tool.PullAsync(args[1], args[2]);
                    break;
                default:
                    Console.WriteLine($"Unrecognized command: {action}");
                    break;
            }
        }
        #endregion

        #region Routines
        /// <summary>
        /// Shared argument handling for `gather` and `archive`, which take the same optional pack scope.
        /// </summary>
        private static void RunPackCommand(string[] args, string commandName, string usage, Action<string, string?, bool> run)
        {
            if (!TryParsePackArguments(args, out string target, out string? subfolder, out bool preserveRepoPaths, out string? error))
            {
                Console.WriteLine(Color.Red, error!);
                Console.WriteLine(Color.Red, usage);
                return;
            }

            run(target, subfolder, preserveRepoPaths);
        }
        /// <summary>
        /// Parse `&lt;target&gt; [--subfolder|-s &lt;path&gt;] [--full-paths]` following the command name.
        /// </summary>
        internal static bool TryParsePackArguments(string[] args, out string target, out string? subfolder, out bool preserveRepoPaths, out string? error)
        {
            target = string.Empty;
            subfolder = null;
            preserveRepoPaths = false;
            error = null;

            List<string> positionals = [];
            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (argument.Equals("--subfolder", StringComparison.OrdinalIgnoreCase) || argument.Equals("-s", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        error = $"Missing value for {argument}.";
                        return false;
                    }

                    subfolder = args[++i];
                }
                else if (argument.Equals("--full-paths", StringComparison.OrdinalIgnoreCase))
                    preserveRepoPaths = true;
                else if (argument.StartsWith("--", StringComparison.Ordinal))
                {
                    error = $"Unrecognized option: {argument}.";
                    return false;
                }
                else
                    positionals.Add(argument);
            }

            if (positionals.Count == 0)
            {
                error = "Missing output path.";
                return false;
            }
            if (positionals.Count > 1)
            {
                error = $"Unexpected extra argument: {positionals[1]}.";
                return false;
            }

            target = positionals[0];
            return true;
        }
        private static void PrintDetailedHelp()
        {
            string helpText = $"""
                cv — Check Version CLI (v{Version})

                Usage:
                  cv <command> [options]

                Commands:
                  init                                  Initialize a new CheckVersion repo in the current directory
                  status                                Show uncommitted file changes (like `git status`)
                  list                                  Show all tracked files (and any uncommitted changes)
                  commit -m <msg>                       Commit current changes with message <msg>
                  log                                   Show commit history
                  gather <output folder>                Gather version-controlled files to a folder
                  archive <output path>                 Compress version-controlled files to an archive
                  checkpoint create <target zip file>   Create a restorable checkpoint archive from a clean repo
                  checkpoint restore <source zip file>  Restore a checkpoint archive into a clean folder
                  push <url> <key>                      Upload new/updated files to CheckVersion-server
                  pull <url> <key>                      Download latest files from CheckVersion-server

                Options for `gather` and `archive`:
                  -s, --subfolder <path>   Pack only tracked files under <path> instead of the whole repo
                      --full-paths         Keep repo-relative paths in the output instead of making
                                           the subfolder the output root (only affects --subfolder)

                Options:
                  -h, --help, help   Show this help information
                  -v, --version      Display version

                Use `.cvignore` to exclude files from tracking. A `.cvignore` may also be placed in any
                subfolder, where its patterns are interpreted relative to that subfolder.
                For push/pull, provide the server base URL (e.g. https://localhost:5001) and your API key.
                """;
            Console.WriteLine(Color.Goldenrod, helpText);
        }
        private static void PrintVersion()
            => Console.WriteLine($"cv — Check Version CLI v{Version}");
        #endregion
    }
}
