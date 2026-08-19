using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CheckVersion.UnitTests
{
    /// <summary>
    /// Packing a single subfolder out of a repo that tracks far more than that subfolder.
    /// </summary>
    public class CheckVersionToolSubfolderPackTests
    {
        [Fact]
        public void Archive_WithSubfolder_PacksOnlyThatFolderAndMakesItTheRoot()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.Tool.Archive(zipPath, subfolder: "small");

                List<string> entries = ReadEntryNames(zipPath);
                Assert.Equal(["nested/two.txt", "one.txt"], entries);
                Assert.Equal("one", ReadEntryText(zipPath, "one.txt"));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithSubfolderAndFullPaths_KeepsRepoRelativeLayout()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.Tool.Archive(zipPath, subfolder: "small", preserveRepoPaths: true);

                Assert.Equal(["small/nested/two.txt", "small/one.txt"], ReadEntryNames(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithNestedSubfolder_PacksOnlyTheDeepestScope()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.Tool.Archive(zipPath, subfolder: "small/nested");

                Assert.Equal(["two.txt"], ReadEntryNames(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithAbsoluteSubfolderInsideRepo_IsAccepted()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.Tool.Archive(zipPath, subfolder: Path.Combine(repo.RootPath, "small"));

                Assert.Equal(["nested/two.txt", "one.txt"], ReadEntryNames(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithSubfolder_IsNotBlockedByFilesMissingElsewhere()
        {
            using TestRepo repo = MakeLargeRepo();

            // A tracked file outside the scope disappears; packing the scope should not care.
            repo.DeleteFile(Path.Combine("big", "deep", "c.txt"));

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.ResetOutput();
                repo.Tool.Archive(zipPath, subfolder: "small");

                Assert.Equal(["nested/two.txt", "one.txt"], ReadEntryNames(zipPath));
                Assert.DoesNotContain(repo.Output.Lines, line => line.Contains("missing"));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithSubfolder_StillFailsWhenAScopedFileIsMissing()
        {
            using TestRepo repo = MakeLargeRepo();

            repo.DeleteFile(Path.Combine("small", "one.txt"));

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.ResetOutput();
                repo.Tool.Archive(zipPath, subfolder: "small");

                Assert.Contains(repo.Output.Lines, line => line.Contains("Cannot archive"));
                Assert.False(File.Exists(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithUnknownSubfolder_ReportsAndCreatesNothing()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.ResetOutput();
                repo.Tool.Archive(zipPath, subfolder: "nope");

                Assert.Contains(repo.Output.Lines, line => line.Contains("no tracked files under 'nope'"));
                Assert.False(File.Exists(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithSubfolderOutsideRepo_IsRejected()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.ResetOutput();
                repo.Tool.Archive(zipPath, subfolder: Path.Combine(repo.RootPath, "..", "elsewhere"));

                Assert.Contains(repo.Output.Lines, line => line.Contains("must be inside the repo"));
                Assert.False(File.Exists(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithControlFolderAsSubfolder_IsRejected()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.ResetOutput();
                repo.Tool.Archive(zipPath, subfolder: ".cv");

                Assert.Contains(repo.Output.Lines, line => line.Contains("control folder"));
                Assert.False(File.Exists(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Archive_WithoutSubfolder_StillPacksEverything()
        {
            using TestRepo repo = MakeLargeRepo();

            string zipPath = repo.SiblingPath(".zip");
            try
            {
                repo.Tool.Archive(zipPath);

                Assert.Equal(
                    ["big/a.txt", "big/deep/c.txt", "small/nested/two.txt", "small/one.txt"],
                    ReadEntryNames(zipPath));
            }
            finally
            {
                Delete(zipPath);
            }
        }

        [Fact]
        public void Gather_WithSubfolder_CopiesOnlyThatFolderAndMakesItTheRoot()
        {
            using TestRepo repo = MakeLargeRepo();

            string outputFolder = repo.SiblingPath();
            try
            {
                repo.Tool.Gather(outputFolder, subfolder: "small");

                Assert.True(File.Exists(Path.Combine(outputFolder, "one.txt")));
                Assert.True(File.Exists(Path.Combine(outputFolder, "nested", "two.txt")));
                Assert.False(Directory.Exists(Path.Combine(outputFolder, "big")));
                Assert.False(Directory.Exists(Path.Combine(outputFolder, "small")));
            }
            finally
            {
                Delete(outputFolder);
            }
        }

        [Fact]
        public void Gather_WithSubfolderAndFullPaths_KeepsRepoRelativeLayout()
        {
            using TestRepo repo = MakeLargeRepo();

            string outputFolder = repo.SiblingPath();
            try
            {
                repo.Tool.Gather(outputFolder, subfolder: "small", preserveRepoPaths: true);

                Assert.True(File.Exists(Path.Combine(outputFolder, "small", "one.txt")));
                Assert.True(File.Exists(Path.Combine(outputFolder, "small", "nested", "two.txt")));
                Assert.False(Directory.Exists(Path.Combine(outputFolder, "big")));
            }
            finally
            {
                Delete(outputFolder);
            }
        }

        /// <summary>
        /// A repo whose tracked content is much larger than the folder we actually want to hand over.
        /// </summary>
        private static TestRepo MakeLargeRepo()
        {
            TestRepo repo = new();

            repo.WriteFile("big/a.txt", "a");
            repo.WriteFile("big/deep/c.txt", "c");
            repo.WriteFile("small/one.txt", "one");
            repo.WriteFile("small/nested/two.txt", "two");

            repo.Tool.Init();
            repo.Tool.Commit("initial");
            return repo;
        }

        private static List<string> ReadEntryNames(string zipPath)
        {
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            return [.. archive.Entries.Select(e => e.FullName.Replace('\\', '/')).OrderBy(e => e, System.StringComparer.Ordinal)];
        }

        private static string ReadEntryText(string zipPath, string entryName)
        {
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            ZipArchiveEntry entry = archive.Entries.Single(e => e.FullName.Replace('\\', '/') == entryName);
            using Stream stream = entry.Open();
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
