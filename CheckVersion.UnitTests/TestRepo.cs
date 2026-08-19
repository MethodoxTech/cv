using CheckVersion.Types;
using System;
using System.IO;

namespace CheckVersion.UnitTests
{
    /// <summary>
    /// A throwaway CV repo on disk, wired to a <see cref="CollectingOutput"/> so assertions can be made
    /// against the tool's messages without redirecting the process-wide console.
    /// </summary>
    internal sealed class TestRepo : IDisposable
    {
        public string RootPath { get; }
        public CheckVersionTool Tool { get; private set; }
        public CollectingOutput Output { get; private set; }

        public TestRepo()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "cv-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);

            Output = new CollectingOutput(confirmAnswer: true);
            Tool = CreateTool(Output);
        }

        /// <summary>
        /// Drop the transcript collected so far, so a test can assert on one operation in isolation.
        /// </summary>
        public void ResetOutput()
        {
            Output = new CollectingOutput(confirmAnswer: true);
            Tool = CreateTool(Output);
        }

        public void WriteFile(string relativePath, string text)
        {
            string fullPath = Path.Combine(RootPath, relativePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, text);

            DateTime timestamp = DateTime.UtcNow.AddSeconds(-10);
            File.SetCreationTimeUtc(fullPath, timestamp);
            File.SetLastWriteTimeUtc(fullPath, timestamp);
        }

        public void DeleteFile(string relativePath)
            => File.Delete(Path.Combine(RootPath, relativePath));

        /// <summary>
        /// A path next to the repo folder, for archive/gather destinations.
        /// </summary>
        public string SiblingPath(string suffix = "")
            => Path.Combine(RootPath, "..", Guid.NewGuid().ToString("N") + suffix);

        private CheckVersionTool CreateTool(CollectingOutput output)
            => new(
                repoRootPath: RootPath,
                repoControlFolderName: RepoDefaults.ControlFolderName,
                repoStorageFilePath: RepoDefaults.StorageFilePath,
                ignoreFilename: RepoDefaults.IgnoreFilename,
                output: output);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
            catch
            {
            }
        }
    }
}
