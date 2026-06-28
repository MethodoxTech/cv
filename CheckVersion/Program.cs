using System;
using System.IO;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using Console = CheckVersion.Types.ColorConsole;

namespace CheckVersion
{
    internal class Program
    {
        #region Constants
        private static readonly string RepoRootPath = Directory.GetCurrentDirectory();
        internal static readonly string RepoStorageFilePath = Path.Combine(RepoControlFolderName, "versions");
        internal const string RepoControlFolderName = ".cv";
        internal const string IgnoreFilename = ".cvignore";
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
                    if (args.Length < 2)
                        Console.WriteLine(Color.Red, "Usage: gather <output folder>");
                    else
                        tool.Gather(args[1]);
                    break;
                case "archive":
                    if (args.Length < 2)
                        Console.WriteLine(Color.Red, "Usage: archive <output zip file>");
                    else
                        tool.Archive(args[1]);
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
        private static void PrintDetailedHelp()
        {
            const string helpText = """
                cv — Check Version CLI (v1.1.0)

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

                Options:
                  -h, --help, help   Show this help information
                  -v, --version      Display version

                Use `.cvignore` to exclude files from tracking.
                For push/pull, provide the server base URL (e.g. https://localhost:5001) and your API key.
                """;
            Console.WriteLine(Color.Goldenrod, helpText);
        }
        private static void PrintVersion()
            => Console.WriteLine("cv — Check Version CLI v1.1.0");
        #endregion
    }
}