using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CheckVersion.GUI.Views;
using CheckVersion.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheckVersion.GUI.UnitTests
{
    /// <summary>
    /// Smoke tests for the procedurally built window. Without XAML there is no compile-time check that the
    /// visual tree is well formed, so these assert that it actually constructs, shows, and refreshes.
    /// </summary>
    public class MainWindowTests
    {
        [AvaloniaFact]
        public async Task MainWindow_NoRepoFolder_ShowsWithoutThrowing()
        {
            using TempRepo repo = new();

            MainWindow window = new(repo.RootPath);
            window.Show();
            await window.PendingRefreshForTest;

            Assert.Contains("No CV repo here yet", window.RepoStateForTest.Text);
            Assert.Empty(Items(window.TrackedListForTest));
        }

        [AvaloniaFact]
        public async Task MainWindow_MissingFolder_ReportsInsteadOfCrashing()
        {
            MainWindow window = new(Path.Combine(Path.GetTempPath(), "cv-gui-tests", Guid.NewGuid().ToString("N")));
            window.Show();
            await window.PendingRefreshForTest;

            Assert.Contains("does not exist", window.RepoStateForTest.Text);
        }

        [AvaloniaFact]
        public async Task MainWindow_CommittedRepo_PopulatesTrackedFilesAndHistory()
        {
            using TempRepo repo = new();

            repo.WriteFile("src/a.txt", "A");
            repo.WriteFile("assets/textures/b.png", "B");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            MainWindow window = new(repo.RootPath);
            window.Show();
            await window.PendingRefreshForTest;

            List<string> tracked = [.. Items(window.TrackedListForTest)];
            Assert.Contains("src/a.txt", tracked);
            Assert.Contains("assets/textures/b.png", tracked);

            Assert.Single(Items(window.HistoryListForTest));
            Assert.Contains("initial", Items(window.HistoryListForTest).Single());

            // The subfolder pick list should offer intermediate folders too.
            List<string> folders = [.. Strings(window.SubfolderBoxForTest.ItemsSource)];
            Assert.Contains("assets", folders);
            Assert.Contains("assets/textures", folders);

            Assert.Contains("2 tracked", window.StatsForTest);
            Assert.Contains("1 commit", window.StatsForTest);
        }

        [AvaloniaFact]
        public async Task MainWindow_DirtyRepo_ShowsPendingChanges()
        {
            using TempRepo repo = new();

            repo.WriteFile("kept.txt", "kept");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            repo.WriteFile("added.txt", "added");
            repo.Delete("kept.txt");

            MainWindow window = new(repo.RootPath);
            window.Show();
            await window.PendingRefreshForTest;

            Assert.Contains(Items(window.NewListForTest), item => item.Contains("added.txt"));
            Assert.Contains(Items(window.DeletedListForTest), item => item.Contains("kept.txt"));
            Assert.Contains("uncommitted", window.StatusTextForTest.Text);
        }

        [AvaloniaFact]
        public async Task MainWindow_SubfolderScope_PreviewsWhatWouldBePacked()
        {
            using TempRepo repo = new();

            repo.WriteFile("big/other.txt", "other");
            repo.WriteFile("small/one.txt", "one");
            repo.WriteFile("small/nested/two.txt", "two");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            MainWindow window = new(repo.RootPath);
            window.Show();
            await window.PendingRefreshForTest;

            Assert.Contains("whole repo", window.PackPreviewForTest.Text);

            window.SubfolderBoxForTest.Text = "small";
            Assert.Contains("Packing 2 files under 'small'", window.PackPreviewForTest.Text);

            // Default drops the scope prefix so the subfolder becomes the output root.
            Assert.Contains("Example output path: nested/two.txt", window.PackPreviewForTest.Text);

            window.SubfolderBoxForTest.Text = "nope";
            Assert.Contains("No tracked files under 'nope'", window.PackPreviewForTest.Text);
        }

        [AvaloniaFact]
        public async Task MainWindow_RefreshAfterExternalChange_PicksUpNewFiles()
        {
            using TempRepo repo = new();

            repo.WriteFile("a.txt", "A");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            MainWindow window = new(repo.RootPath);
            window.Show();
            await window.PendingRefreshForTest;
            Assert.Single(Items(window.TrackedListForTest));

            repo.WriteFile("b.txt", "B");
            repo.Tool.Commit("second");
            await window.RefreshForTest();

            Assert.Equal(2, Items(window.TrackedListForTest).Count);
            Assert.Equal(2, Items(window.HistoryListForTest).Count);
        }

        /// <summary>
        /// The regression test for the freeze: reading a repo must not happen on the UI thread. Right after
        /// the window is constructed and shown, the read is still only pending — a synchronous read would
        /// have finished it (and, in the real app, kept the window from appearing at all until it did).
        /// </summary>
        [AvaloniaFact]
        public async Task MainWindow_OpeningRepo_ReadsOffTheUiThread()
        {
            using TempRepo repo = new();

            repo.WriteFile("a.txt", "A");
            repo.Tool.Init();
            repo.Tool.Commit("initial");

            MainWindow window = new(repo.RootPath);
            window.Show();

            Assert.Contains("Reading repo", window.StatusTextForTest.Text);
            Assert.Empty(Items(window.TrackedListForTest));

            await window.PendingRefreshForTest;

            Assert.Single(Items(window.TrackedListForTest));
            Assert.Contains("clean", window.StatusTextForTest.Text);
        }

        /// <summary>
        /// Switching to another repo while a slow read is still running must end on the repo the user
        /// actually asked for, not on whichever read happens to finish last.
        /// </summary>
        [AvaloniaFact]
        public async Task MainWindow_RepoSwitchedMidRead_KeepsTheNewestRepo()
        {
            using TempRepo slow = new();
            using TempRepo wanted = new();

            // Deliberately the slower repo to read, so its snapshot lands after the one that superseded it.
            for (int i = 0; i < 2000; i++)
                slow.WriteFile($"bulk/file{i}.txt", "1");
            slow.Tool.Init();
            slow.Tool.Commit("bulk");

            wanted.WriteFile("wanted-a.txt", "2");
            wanted.WriteFile("wanted-b.txt", "2");
            wanted.Tool.Init();
            wanted.Tool.Commit("wanted");

            MainWindow window = new(wanted.RootPath);
            window.Show();
            await window.PendingRefreshForTest;

            // Two reads started back to back, with nothing in between that could let the first one land.
            window.RepoPathBoxForTest.Text = slow.RootPath;
            Task slowRead = window.RefreshForTest();
            window.RepoPathBoxForTest.Text = wanted.RootPath;
            Task wantedRead = window.RefreshForTest();

            // Both have to finish before the state can be judged: the point is that the superseded read
            // lands last and still does not win.
            await Task.WhenAll(slowRead, wantedRead);

            List<string> tracked = [.. Items(window.TrackedListForTest)];
            Assert.Equal(2, tracked.Count);
            Assert.Contains("wanted-a.txt", tracked);
        }

        private static List<string> Items(ListBox list)
            => [.. Strings(list.ItemsSource)];

        private static IEnumerable<string> Strings(IEnumerable? source)
            => source?.Cast<object>().Select(item => item?.ToString() ?? string.Empty) ?? [];

        private sealed class TempRepo : IDisposable
        {
            public string RootPath { get; }
            public CheckVersionTool Tool { get; }

            public TempRepo()
            {
                RootPath = Path.Combine(Path.GetTempPath(), "cv-gui-tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(RootPath);

                Tool = new CheckVersionTool(
                    repoRootPath: RootPath,
                    repoControlFolderName: RepoDefaults.ControlFolderName,
                    repoStorageFilePath: RepoDefaults.StorageFilePath,
                    ignoreFilename: RepoDefaults.IgnoreFilename,
                    output: new CollectingOutput(confirmAnswer: true));
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

            public void Delete(string relativePath)
                => File.Delete(Path.Combine(RootPath, relativePath));

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
