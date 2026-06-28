using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CheckVersion.UnitTests
{
    public class CheckVersionToolArchiveGatherCheckpointTests
    {
        [Fact]
        public void Gather_CleanRepo_CopiesTrackedFiles()
        {
            using TempRepo repo = new();

            repo.WriteFile("src/a.txt", "A");
            repo.WriteFile("src/b.txt", "B");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            string outputFolder = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N"));

            try
            {
                repo.Tool.Gather(outputFolder);

                Assert.True(File.Exists(Path.Combine(outputFolder, "src", "a.txt")));
                Assert.True(File.Exists(Path.Combine(outputFolder, "src", "b.txt")));
                Assert.Equal("A", File.ReadAllText(Path.Combine(outputFolder, "src", "a.txt")));
                Assert.Equal("B", File.ReadAllText(Path.Combine(outputFolder, "src", "b.txt")));
                Assert.False(Directory.Exists(Path.Combine(outputFolder, ".cv")));
            }
            finally
            {
                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, recursive: true);
            }
        }

        [Fact]
        public void Gather_DirtyRepo_WarnsButStillCopiesTrackedFilesOnly()
        {
            using TempRepo repo = new();

            repo.WriteFile("tracked.txt", "tracked");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            repo.WriteFile("untracked.txt", "untracked");

            string outputFolder = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N"));

            try
            {
                string output = CaptureOutput(() => repo.Tool.Gather(outputFolder));

                Assert.Contains("Warning", output);
                Assert.True(File.Exists(Path.Combine(outputFolder, "tracked.txt")));
                Assert.False(File.Exists(Path.Combine(outputFolder, "untracked.txt")));
            }
            finally
            {
                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, recursive: true);
            }
        }

        [Fact]
        public void Archive_CleanRepo_CreatesZipWithTrackedFilesOnly()
        {
            using TempRepo repo = new();

            repo.WriteFile("src/a.txt", "A");
            repo.WriteFile("src/b.txt", "B");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            string zipPath = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                repo.Tool.Archive(zipPath);

                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                Assert.True(HasEntry(archive, "src/a.txt"));
                Assert.True(HasEntry(archive, "src/b.txt"));
                Assert.False(HasEntry(archive, ".cv/versions"));
                Assert.Equal("A", ReadEntryText(archive, "src/a.txt"));
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_DirtyRepo_WarnsAndArchivesCurrentTrackedFileContent()
        {
            using TempRepo repo = new();

            repo.WriteFile("tracked.txt", "before");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            repo.UpdateFile("tracked.txt", "after");
            repo.WriteFile("untracked.txt", "untracked");

            string zipPath = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                string output = CaptureOutput(() => repo.Tool.Archive(zipPath));

                Assert.Contains("Warning", output);

                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                Assert.True(HasEntry(archive, "tracked.txt"));
                Assert.False(HasEntry(archive, "untracked.txt"));
                Assert.False(HasEntry(archive, ".cv/versions"));
                Assert.Equal("after", ReadEntryText(archive, "tracked.txt"));
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }

        [Fact]
        public void CheckpointCreate_CleanRepo_CreatesZipWithHistoryAndTrackedFiles()
        {
            using TempRepo repo = new();

            repo.WriteFile("src/a.txt", "A");
            repo.WriteFile("src/b.txt", "B");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            string zipPath = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                repo.Tool.CreateCheckpoint(zipPath);

                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                Assert.True(HasEntry(archive, ".cv/versions"));
                Assert.True(HasEntry(archive, "src/a.txt"));
                Assert.True(HasEntry(archive, "src/b.txt"));
                Assert.Equal("A", ReadEntryText(archive, "src/a.txt"));
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }

        [Fact]
        public void CheckpointCreate_DirtyRepo_DoesNotCreateZip()
        {
            using TempRepo repo = new();

            repo.WriteFile("tracked.txt", "before");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            repo.UpdateFile("tracked.txt", "after");

            string zipPath = Path.Combine(repo.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                string output = CaptureOutput(() => repo.Tool.CreateCheckpoint(zipPath));

                Assert.Contains("Cannot create checkpoint", output);
                Assert.False(File.Exists(zipPath));
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }

        [Fact]
        public void CheckpointRestore_CleanFolder_RestoresHistoryAndFiles()
        {
            using TempRepo source = new();

            source.WriteFile("src/a.txt", "A");
            source.WriteFile("src/b.txt", "B");
            source.Tool.Init();
            source.Tool.Commit("initial");

            string zipPath = Path.Combine(source.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");
            string restoreRoot = Path.Combine(source.RootPath, "..", Guid.NewGuid().ToString("N"));

            try
            {
                source.Tool.CreateCheckpoint(zipPath);

                Directory.CreateDirectory(restoreRoot);
                CheckVersionTool restoredTool = new(
                    repoRootPath: restoreRoot,
                    repoControlFolderName: ".cv",
                    repoStorageFilePath: Path.Combine(".cv", "versions"),
                    ignoreFilename: ".cvignore"
                );

                restoredTool.RestoreCheckpoint(zipPath);

                Assert.True(File.Exists(Path.Combine(restoreRoot, ".cv", "versions")));
                Assert.Equal("A", File.ReadAllText(Path.Combine(restoreRoot, "src", "a.txt")));
                Assert.Equal("B", File.ReadAllText(Path.Combine(restoreRoot, "src", "b.txt")));

                string status = CaptureOutput(() => restoredTool.Status());
                Assert.Contains("# New: 0", status);
                Assert.Contains("# Updated: 0", status);
                Assert.Contains("# Moved: 0", status);
                Assert.Contains("# Deleted: 0", status);
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                if (Directory.Exists(restoreRoot))
                    Directory.Delete(restoreRoot, recursive: true);
            }
        }

        [Fact]
        public void CheckpointRestore_ExistingRepo_RefusesRestore()
        {
            using TempRepo source = new();
            using TempRepo destination = new();

            source.WriteFile("source.txt", "source");
            source.Tool.Init();
            source.Tool.Commit("initial");

            destination.Tool.Init();

            string zipPath = Path.Combine(source.RootPath, "..", Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                source.Tool.CreateCheckpoint(zipPath);

                string output = CaptureOutput(() => destination.Tool.RestoreCheckpoint(zipPath));

                Assert.Contains("already exists", output);
                Assert.False(File.Exists(Path.Combine(destination.RootPath, "source.txt")));
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }

        private static bool HasEntry(ZipArchive archive, string path)
            => archive.Entries.Any(e => e.FullName.Replace('\\', '/') == path);

        private static string ReadEntryText(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.Entries.Single(e => e.FullName.Replace('\\', '/') == path);
            using Stream stream = entry.Open();
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static string CaptureOutput(Action action)
        {
            TextWriter previous = Console.Out;
            using StringWriter writer = new();

            Console.SetOut(writer);
            try
            {
                action();
            }
            finally
            {
                Console.SetOut(previous);
            }

            return writer.ToString();
        }

        private sealed class TempRepo : IDisposable
        {
            public string RootPath { get; }
            public CheckVersionTool Tool { get; }

            public TempRepo()
            {
                RootPath = Path.Combine(Path.GetTempPath(), "cv-tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(RootPath);

                Tool = new CheckVersionTool(
                    repoRootPath: RootPath,
                    repoControlFolderName: ".cv",
                    repoStorageFilePath: Path.Combine(".cv", "versions"),
                    ignoreFilename: ".cvignore"
                );
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

            public void UpdateFile(string relativePath, string text)
            {
                string fullPath = Path.Combine(RootPath, relativePath);
                File.WriteAllText(fullPath, text);
                File.SetLastWriteTimeUtc(fullPath, DateTime.UtcNow.AddMinutes(1));
            }

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
}