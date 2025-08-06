using System;
using System.IO;
using Color = System.Drawing.Color;
using Console = cv.Types.ColorConsole;

namespace cv
{
    internal class Program
    {
        #region Constants
        private static readonly string RepoRootPath = Directory.GetCurrentDirectory();
        private static readonly string RepoStorageFilePath = Path.Combine(RepoControlFolderName, "versions");
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
                    Console.WriteLine(Color.DarkGreen, $"Usage: cv status|init|list|commit|log -m <Message>|log");
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

            ChangeVersionTool tool = new(RepoRootPath, RepoControlFolderName, RepoStorageFilePath, IgnoreFilename);
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
                    if (args.Length != 3)
                        Console.WriteLine(Color.Red, "commit -m <Message>");
                    else
                        tool.Commit(args[2]);
                    break;
                case "log":
                    tool.Log();
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
                  list               Show all tracked files (and any uncommitted changes)
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
        #endregion
    }
}