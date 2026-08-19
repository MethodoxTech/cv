using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CheckVersion.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DrawingColor = System.Drawing.Color;
using Path = System.IO.Path;

namespace CheckVersion.GUI.Views
{
    /// <summary>
    /// The whole UI, assembled procedurally. No XAML, no bindings, no MVVM framework: controls are held as
    /// fields and refreshed directly, which keeps the data flow obvious for a tool this size.
    /// </summary>
    public sealed class MainWindow : Window
    {
        #region Fields
        private readonly TextBox _repoPathBox;
        private readonly TextBlock _repoStateText;
        private readonly StackPanel _statChips;
        private List<string> _stats = [];
        private readonly Button _initButton;

        private readonly ChangeSection _new;
        private readonly ChangeSection _updated;
        private readonly ChangeSection _moved;
        private readonly ChangeSection _deleted;
        private readonly TextBox _commitMessageBox;
        private readonly Button _commitButton;

        private readonly ListBox _trackedList;
        private readonly TextBlock _trackedEmpty;
        private readonly TextBox _trackedFilterBox;
        private readonly TextBlock _trackedSummary;
        private List<string> _trackedFiles = [];
        // Built off the UI thread together with the rest of the snapshot, so filtering never has to stat thousands of files again on every keystroke.
        private List<TrackedItem> _trackedItems = [];

        private readonly ListBox _historyList;
        private readonly TextBlock _historyEmpty;

        private readonly AutoCompleteBox _subfolderBox;
        private readonly CheckBox _fullPathsCheck;
        private readonly TextBlock _packPreview;

        private readonly OutputLogView _log;
        private readonly UiOutput _output;
        private readonly TextBlock _statusText;
        private readonly Ellipse _statusDot;
        private readonly ProgressBar _busyBar;
        private readonly Panel _busyOverlay;
        private readonly StackPanel _repoButtons;

        private bool _isBusy;

        /// <summary>
        /// Identifies the newest requested read. A snapshot that comes back carrying an older number was superseded (the user switched repos while it was running) and is dropped instead of applied.
        /// </summary>
        private int _refreshGeneration;
        /// <summary>
        /// The read currently in flight, so tests (and <see cref="RunToolOperation"/>) can wait for the window to catch up with the disk.
        /// </summary>
        private Task _refreshTask = Task.CompletedTask;
        #endregion

        #region Constants
        /// <summary>
        /// Held once rather than rebuilt per toggle, since a Cursor owns a native handle.
        /// </summary>
        private static readonly Cursor WaitCursor = new(StandardCursorType.Wait);
        /// <summary>
        /// Version of the tool assembly this front-end is driving, so the two cannot be reported out of step.
        /// </summary>
        private static string ToolVersion
            => typeof(CheckVersionTool).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        #endregion

        #region Construction
        public MainWindow(string initialRepoPath)
        {
            Title = "Check Version";
            Width = 1140;
            Height = 800;
            MinWidth = 860;
            MinHeight = 560;
            Background = AppTheme.BackgroundBrush;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _log = new OutputLogView();
            _output = new UiOutput(_log);

            _repoPathBox = new TextBox { PlaceholderText = "Path to a folder containing a .cv repo" };
            _repoStateText = Ui.Caption(string.Empty);
            _statChips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            _initButton = Ui.Action("Init Repo", Ui.IconPlus, accent: true);

            _new = new ChangeSection("New", AppTheme.NewBrush, "Nothing new");
            _updated = new ChangeSection("Updated", AppTheme.UpdatedBrush, "Nothing updated");
            _moved = new ChangeSection("Moved", AppTheme.MovedBrush, "Nothing moved");
            _deleted = new ChangeSection("Deleted", AppTheme.DeletedBrush, "Nothing deleted");

            _commitMessageBox = new TextBox { PlaceholderText = "Describe this commit…", AcceptsReturn = false };
            _commitButton = Ui.Action("Commit", Ui.IconCommit, accent: true, minWidth: 120);

            _trackedList = new ListBox { SelectionMode = SelectionMode.Single, ItemTemplate = Ui.TrackedTemplate() };
            _trackedEmpty = Ui.EmptyHint("No tracked files");
            _trackedFilterBox = new TextBox { PlaceholderText = "Filter tracked files…" };
            _trackedSummary = new TextBlock
            {
                Classes = { "caption" },
                VerticalAlignment = VerticalAlignment.Center
            };

            _historyList = new ListBox { SelectionMode = SelectionMode.Single, ItemTemplate = Ui.CommitTemplate() };
            _historyEmpty = Ui.EmptyHint("No commits yet");

            _subfolderBox = new AutoCompleteBox
            {
                PlaceholderText = "(whole repo)",
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
                MinimumPrefixLength = 0
            };
            _fullPathsCheck = new CheckBox { Content = "Keep full repo-relative paths in output" };
            _packPreview = Ui.Caption(string.Empty);

            _statusText = new TextBlock
            {
                Classes = { "caption" },
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusDot = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = AppTheme.TextFaintBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            // Indeterminate only while an operation runs: the animation is a perpetual dispatcher job, so
            // leaving it on would spin forever behind a hidden control.
            _busyBar = new ProgressBar { IsIndeterminate = false, IsVisible = false, Width = 110, Height = 3 };

            // A transparent lid over the working area is the simplest reliable way to keep a background
            // operation from being started twice or racing a repo switch.
            _busyOverlay = new Panel { Background = Brushes.Transparent, IsVisible = false, IsHitTestVisible = true };

            _repoButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(10, 0, 0, 0)
            };

            Content = BuildLayout();
            WireEvents();

            _output.WriteLine(DrawingColor.Goldenrod, $"Check Version — GUI front-end for cv v{ToolVersion}");
            OpenRepo(initialRepoPath, announce: true);
        }
        #endregion

        #region Test Access
        // Narrow hooks for the headless UI tests. Procedurally built trees have no XAML name scope to look
        // controls up through, and these keep the tests from reaching into private state by reflection.
        internal TextBox RepoPathBoxForTest => _repoPathBox;
        internal TextBlock RepoStateForTest => _repoStateText;
        internal IReadOnlyList<string> StatsForTest => _stats;
        internal TextBlock StatusTextForTest => _statusText;
        internal TextBlock PackPreviewForTest => _packPreview;
        internal ListBox NewListForTest => _new.List;
        internal ListBox UpdatedListForTest => _updated.List;
        internal ListBox DeletedListForTest => _deleted.List;
        internal ListBox TrackedListForTest => _trackedList;
        internal ListBox HistoryListForTest => _historyList;
        internal AutoCompleteBox SubfolderBoxForTest => _subfolderBox;
        /// <summary>
        /// Re-read the repo and hand back the task, since reading now happens off the UI thread.
        /// </summary>
        internal Task RefreshForTest()
        {
            Refresh();
            return _refreshTask;
        }
        /// <summary>
        /// The read started by the constructor (or the last one requested), so a test can wait for the window to be populated instead of racing it.
        /// </summary>
        internal Task PendingRefreshForTest => _refreshTask;
        #endregion

        #region Layout
        private Control BuildLayout()
        {
            Grid workspace = new()
            {
                Margin = new Thickness(16, 12, 16, 0),
                // The log starts modest; the splitter below lets it be dragged open.
                RowDefinitions = new RowDefinitions("*,Auto,132")
            };

            TabControl tabs = new()
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 12, 0, 0),
                Items =
                {
                    new TabItem { Header = "Status", Content = BuildStatusTab() },
                    new TabItem { Header = "Tracked Files", Content = BuildTrackedTab() },
                    new TabItem { Header = "History", Content = BuildHistoryTab() },
                    new TabItem { Header = "Pack", Content = BuildPackTab() }
                }
            };

            Grid.SetRow(tabs, 0);
            workspace.Children.Add(tabs);

            GridSplitter splitter = new()
            {
                Height = 5,
                Margin = new Thickness(0, 9, 0, 8),
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(splitter, 1);
            workspace.Children.Add(splitter);

            Control logPanel = BuildLogPanel();
            Grid.SetRow(logPanel, 2);
            workspace.Children.Add(logPanel);

            DockPanel root = new();
            root.Children.Add(Ui.Docked(BuildHeader(), Dock.Top));
            root.Children.Add(Ui.Docked(BuildStatusBar(), Dock.Bottom));
            root.Children.Add(new Panel { Children = { workspace, _busyOverlay } });
            return root;
        }

        private Control BuildHeader()
        {
            Button browse = Ui.Quiet("Browse", Ui.IconFolder);
            browse.Click += async (_, _) =>
            {
                string? picked = await Dialogs.PickFolderAsync(this, "Select a CV repo folder", _repoPathBox.Text);
                if (picked != null)
                    OpenRepo(picked, announce: true);
            };

            Button open = Ui.Quiet("Open", Ui.IconOpen);
            open.Click += (_, _) => OpenRepo(_repoPathBox.Text ?? string.Empty, announce: true);

            Button refresh = Ui.Quiet("Refresh", Ui.IconRefresh);
            refresh.Click += (_, _) => Refresh();

            _initButton.Click += (_, _) => RunToolOperation("Initialize repo", tool => tool.Init());

            // The busy lid only covers the workspace, so these live above it and are disabled explicitly
            // instead — otherwise a repo switch could be started on top of a running operation.
            _repoButtons.Children.Add(browse);
            _repoButtons.Children.Add(open);
            _repoButtons.Children.Add(refresh);
            _repoButtons.Children.Add(_initButton);

            DockPanel pathRow = new();
            pathRow.Children.Add(Ui.Docked(_repoButtons, Dock.Right));
            pathRow.Children.Add(_repoPathBox);

            StackPanel identity = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 10),
                Children =
                {
                    Ui.Title("Check Version"),
                    new TextBlock
                    {
                        Text = $"v{ToolVersion}",
                        FontSize = AppTheme.FontSmall,
                        Foreground = AppTheme.TextFaintBrush,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 2)
                    }
                }
            };

            DockPanel infoRow = new() { Margin = new Thickness(0, 10, 0, 0) };
            infoRow.Children.Add(Ui.Docked(_statChips, Dock.Right));
            infoRow.Children.Add(_repoStateText);

            return new Border
            {
                Background = AppTheme.SurfaceBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 14),
                Child = new StackPanel { Children = { identity, pathRow, infoRow } }
            };
        }

        private Control BuildStatusTab()
        {
            Grid changes = new()
            {
                RowDefinitions = new RowDefinitions("*,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*")
            };

            AddCell(changes, _new.Card, 0, 0);
            AddCell(changes, _updated.Card, 0, 1);
            AddCell(changes, _moved.Card, 1, 0);
            AddCell(changes, _deleted.Card, 1, 1);

            _commitButton.Click += (_, _) => CommitAsync();

            DockPanel commitRow = new();
            commitRow.Children.Add(Ui.Docked(_commitButton, Dock.Right));
            _commitMessageBox.Margin = new Thickness(0, 0, 10, 0);
            commitRow.Children.Add(_commitMessageBox);

            Border commitBar = new()
            {
                Margin = new Thickness(0, 12, 0, 0),
                Background = AppTheme.SurfaceBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(AppTheme.CornerLarge),
                Padding = new Thickness(12),
                Child = commitRow
            };

            DockPanel panel = new();
            panel.Children.Add(Ui.Docked(commitBar, Dock.Bottom));
            panel.Children.Add(changes);
            return panel;
        }

        private Control BuildTrackedTab()
        {
            DockPanel filterRow = new();
            filterRow.Children.Add(Ui.Docked(_trackedSummary, Dock.Right));
            _trackedSummary.Margin = new Thickness(12, 0, 0, 0);
            filterRow.Children.Add(_trackedFilterBox);

            return Ui.Card(filterRow, new Panel { Children = { _trackedList, _trackedEmpty } });
        }

        private Control BuildHistoryTab()
            => Ui.Card(
                Ui.CountHeader("Commits", AppTheme.AccentBrush, new TextBlock { Classes = { "caption" } }),
                new Panel { Children = { _historyList, _historyEmpty } });

        private Control BuildPackTab()
        {
            Button pickSubfolder = Ui.Action("Browse", Ui.IconFolder);
            pickSubfolder.Click += async (_, _) =>
            {
                string? picked = await Dialogs.PickFolderAsync(this, "Select a subfolder inside the repo", _repoPathBox.Text);
                if (picked == null)
                    return;

                if (TryMakeRepoRelative(picked, out string relative))
                    _subfolderBox.Text = relative;
                else
                    SetStatus("Selected folder is not inside the repo.", StatusKind.Error);
            };

            Button clearSubfolder = Ui.Action("Whole repo");
            clearSubfolder.Click += (_, _) => _subfolderBox.Text = string.Empty;

            DockPanel subfolderRow = new();
            subfolderRow.Children.Add(Ui.Docked(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(10, 0, 0, 0),
                Children = { pickSubfolder, clearSubfolder }
            }, Dock.Right));
            subfolderRow.Children.Add(_subfolderBox);

            Button gather = Ui.Action("Gather to Folder", Ui.IconFolderOut, accent: true, minWidth: 175);
            gather.Click += (_, _) => GatherAsync();

            Button archive = Ui.Action("Archive to Zip", Ui.IconZip, accent: true, minWidth: 175);
            archive.Click += (_, _) => ArchiveAsync();

            Button checkpointCreate = Ui.Action("Create Checkpoint", Ui.IconCheckpoint, minWidth: 175);
            checkpointCreate.Click += (_, _) => CreateCheckpointAsync();

            Button checkpointRestore = Ui.Action("Restore Checkpoint", Ui.IconRestore, minWidth: 175);
            checkpointRestore.Click += (_, _) => RestoreCheckpointAsync();

            Border scopeCard = Ui.Card(
                Ui.CountHeader("Scope", AppTheme.AccentBrush, new TextBlock { Classes = { "caption" } }),
                new StackPanel
                {
                    Margin = new Thickness(8),
                    Spacing = 10,
                    Children =
                    {
                        Ui.Caption("Leave empty to pack the whole repo. Pick a subfolder to pack only the tracked files under it — useful when a large repo is CV-tracked but you only want to hand over one folder."),
                        subfolderRow,
                        _fullPathsCheck,
                        new Border
                        {
                            Background = AppTheme.SurfaceSunkenBrush,
                            BorderBrush = AppTheme.LineBrush,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(AppTheme.CornerMedium),
                            Padding = new Thickness(10, 8),
                            Child = _packPreview
                        }
                    }
                });

            Border packCard = Ui.Card(
                Ui.CountHeader("Pack tracked files", AppTheme.NewBrush, new TextBlock { Classes = { "caption" } }),
                new StackPanel
                {
                    Margin = new Thickness(8),
                    Spacing = 10,
                    Children =
                    {
                        Ui.Caption("Copies the current contents of tracked files. `.cv` history is not included."),
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children = { gather, archive }
                        }
                    }
                },
                new Thickness(0, 0, 0, 10));

            Border checkpointCard = Ui.Card(
                Ui.CountHeader("Checkpoints", AppTheme.MovedBrush, new TextBlock
                {
                    Classes = { "caption" },
                    Text = "whole repo only"
                }),
                new StackPanel
                {
                    Margin = new Thickness(8),
                    Spacing = 10,
                    Children =
                    {
                        Ui.Caption("A checkpoint is a restorable snapshot that includes `.cv` history, so it always covers the whole repo — the stored records are repo-root relative. Requires a clean repo."),
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children = { checkpointCreate, checkpointRestore }
                        }
                    }
                });

            // Two columns so the scope and both action groups stay visible together on a laptop screen,
            // rather than pushing the checkpoint buttons below the fold.
            Grid layout = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,10,420"),
                RowDefinitions = new RowDefinitions("Auto,*")
            };

            Grid.SetColumn(scopeCard, 0);
            Grid.SetRowSpan(scopeCard, 2);
            layout.Children.Add(scopeCard);

            StackPanel actions = new() { Children = { packCard, checkpointCard } };
            Grid.SetColumn(actions, 2);
            layout.Children.Add(actions);

            return new ScrollViewer { Content = layout };
        }

        private Control BuildLogPanel()
        {
            Button clearLog = Ui.Quiet("Clear", Ui.IconClear);
            clearLog.Click += (_, _) => _log.Clear();

            DockPanel header = new();
            header.Children.Add(Ui.Docked(clearLog, Dock.Right));
            header.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    Ui.Heading("Output"),
                    _busyBar
                }
            });

            return new Border
            {
                Background = AppTheme.SurfaceBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(AppTheme.CornerLarge),
                Child = new DockPanel
                {
                    Children =
                    {
                        Ui.Docked(new Border
                        {
                            Padding = new Thickness(14, 6),
                            BorderBrush = AppTheme.LineBrush,
                            BorderThickness = new Thickness(0, 0, 0, 1),
                            Child = header
                        }, Dock.Top),
                        _log
                    }
                }
            };
        }

        private Control BuildStatusBar()
            => new Border
            {
                Background = AppTheme.SurfaceBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 8),
                Margin = new Thickness(0, 12, 0, 0),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 9,
                    Children = { _statusDot, _statusText }
                }
            };

        private static void AddCell(Grid grid, Control control, int row, int column)
        {
            Grid.SetRow(control, row);
            Grid.SetColumn(control, column);
            grid.Children.Add(control);
        }
        #endregion

        #region Events
        private void WireEvents()
        {
            _trackedFilterBox.TextChanged += (_, _) => ApplyTrackedFilter();
            _fullPathsCheck.IsCheckedChanged += (_, _) => UpdatePackPreview();

            // AutoCompleteBox raises TextChanged from its inner editor, so it stays silent when the text is
            // set in code (the "Whole repo" button, or the subfolder browser). Watching the property itself
            // catches both typed and programmatic changes.
            _subfolderBox.PropertyChanged += (_, e) =>
            {
                if (e.Property == AutoCompleteBox.TextProperty)
                    UpdatePackPreview();
            };
        }
        #endregion

        #region Repo Operations
        private CheckVersionTool CreateTool(string rootPath)
            => new(
                repoRootPath: rootPath,
                repoControlFolderName: RepoDefaults.ControlFolderName,
                repoStorageFilePath: RepoDefaults.StorageFilePath,
                ignoreFilename: RepoDefaults.IgnoreFilename,
                output: _output);

        private string RepoPath
            => (_repoPathBox.Text ?? string.Empty).Trim();

        private void OpenRepo(string path, bool announce)
        {
            _repoPathBox.Text = path;

            if (announce)
                _output.WriteLine(DrawingColor.Cyan, $"# Repo: {path}");

            Refresh();
        }

        /// <summary>
        /// Re-read everything the window shows from disk.
        /// </summary>
        /// <remarks>
        /// Nothing here is cheap on a real repo: the changelist walks every folder and the stored history has to be deserialized, which together take seconds on a repo with tens of thousands of files. 
        /// Doing that on the UI thread is what made opening a repo look like a hang — the window simply stopped painting until the walk finished (and, at startup, never appeared at all). The read runs on the thread pool instead, and only the finished snapshot is applied here.
        /// </remarks>
        private void Refresh()
            => _refreshTask = RefreshAsync();

        private async Task RefreshAsync()
        {
            string path = RepoPath;
            int generation = ++_refreshGeneration;

            SetReading(true);
            RepoSnapshot snapshot;
            try
            {
                snapshot = await Task.Run(() => ReadSnapshot(path));
            }
            catch (Exception ex)
            {
                // ReadSnapshot already reports repo-level failures; this only catches a broken read itself.
                snapshot = RepoSnapshot.Failed(ex.Message);
            }

            // A slower read of a repo the user has already navigated away from must not overwrite the newer one that replaced it.
            if (generation != _refreshGeneration)
                return;

            SetReading(false);
            Apply(snapshot);
        }

        /// <summary>
        /// Read the whole repo state off the UI thread. Touches no control, so everything it produces is plain data the UI thread can apply directly.
        /// </summary>
        private RepoSnapshot ReadSnapshot(string path)
        {
            // Even these two probes can block (a disconnected network share), so they belong here too.
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return RepoSnapshot.NoRepo($"Folder does not exist: {path}", canInit: false);

            CheckVersionTool tool = CreateTool(path);
            if (!tool.RepoExists)
                return RepoSnapshot.NoRepo("No CV repo here yet — use Init Repo to create one.", canInit: true);

            try
            {
                // One deserialization of the stored history feeds the changelist, the tracked list, the folder suggestions and the commit list, instead of the four this used to cost.
                RepoHistory history = tool.GetHistory();
                Changelist changes = tool.GetChangelist(history);
                List<string> trackedFiles = CheckVersionTool.GetTrackedFiles(history);

                return new RepoSnapshot
                {
                    State = RepoState.Ready,
                    Changes = changes,
                    TrackedFiles = trackedFiles,
                    // Whether a tracked file is still on disk is one stat call each: done here, the filter box stays instant no matter how large the repo is.
                    TrackedItems = [.. trackedFiles.Select(p => new TrackedItem
                    {
                        Path = p,
                        IsMissing = !File.Exists(Path.Combine(path, p))
                    })],
                    Folders = CheckVersionTool.GetTrackedFolders(history),
                    Commits = [.. history.Commits
                        .Select((commit, index) => new CommitItem
                        {
                            Index = index,
                            Message = string.IsNullOrWhiteSpace(commit.Message) ? "(no message)" : commit.Message,
                            Time = commit.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                            ChangeCount = commit.Changes.Count
                        })
                        .Reverse()]
                };
            }
            catch (Exception ex)
            {
                return RepoSnapshot.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Put a finished snapshot on screen. UI thread only.
        /// </summary>
        private void Apply(RepoSnapshot snapshot)
        {
            if (snapshot.State == RepoState.NoRepo)
            {
                ShowNoRepo(snapshot.Message, snapshot.CanInit);
                return;
            }

            if (snapshot.State == RepoState.Failed)
            {
                ShowNoRepo($"Failed to read repo: {snapshot.Message}", canInit: false);
                _output.WriteLine(DrawingColor.Red, $"Failed to read repo: {snapshot.Message}");
                return;
            }

            Changelist changes = snapshot.Changes!;

            _initButton.IsEnabled = false;
            _commitButton.IsEnabled = true;

            _new.Fill([.. changes.NewFiles.Select(f => Describe(f, AppTheme.NewBrush))]);
            _updated.Fill([.. changes.UpdatedFiles.Select(f => Describe(f, AppTheme.UpdatedBrush))]);
            _moved.Fill([.. changes.MovedFiles.Select(f => Describe(f, AppTheme.MovedBrush))]);
            _deleted.Fill([.. changes.DeletedFiles.Select(f => Describe(f, AppTheme.DeletedBrush))]);

            _trackedFiles = snapshot.TrackedFiles;
            _trackedItems = snapshot.TrackedItems;
            ApplyTrackedFilter();

            _historyList.ItemsSource = snapshot.Commits;
            _historyEmpty.IsVisible = snapshot.Commits.Count == 0;

            _subfolderBox.ItemsSource = snapshot.Folders;

            int changeCount = changes.NewFiles.Count + changes.UpdatedFiles.Count + changes.MovedFiles.Count + changes.DeletedFiles.Count;

            // The chips carry the healthy-state numbers, so this line is reserved for problems.
            SetRepoState(string.Empty);
            ShowChips(_trackedFiles.Count, snapshot.Commits.Count, changeCount);
            SetStatus(
                changeCount == 0 ? "Repo is clean." : $"{changeCount} uncommitted {(changeCount == 1 ? "change" : "changes")}.",
                changeCount == 0 ? StatusKind.Good : StatusKind.Pending);

            UpdatePackPreview();
        }

        /// <summary>
        /// Show that a read is under way. The window stays interactive, but the repo buttons are held so a second read cannot be stacked on the first.
        /// </summary>
        private void SetReading(bool reading)
        {
            _busyBar.IsIndeterminate = reading || _isBusy;
            _busyBar.IsVisible = reading || _isBusy;
            _repoButtons.IsEnabled = !reading && !_isBusy;

            if (reading)
                SetStatus("Reading repo…", StatusKind.Pending);
        }

        private void ShowNoRepo(string message, bool canInit)
        {
            _initButton.IsEnabled = canInit;
            _commitButton.IsEnabled = false;
            _trackedFiles = [];
            _trackedItems = [];

            _new.Fill([]);
            _updated.Fill([]);
            _moved.Fill([]);
            _deleted.Fill([]);

            _trackedList.ItemsSource = null;
            _trackedEmpty.IsVisible = true;
            _historyList.ItemsSource = null;
            _historyEmpty.IsVisible = true;
            _subfolderBox.ItemsSource = null;
            _trackedSummary.Text = string.Empty;
            SetRepoState(message);
            _packPreview.Text = string.Empty;
            _statChips.Children.Clear();

            SetStatus(message, canInit ? StatusKind.Pending : StatusKind.Error);
        }

        /// <summary>
        /// The line under the repo box. Only shown when there is a problem to report, since the stat chips
        /// already cover the healthy case.
        /// </summary>
        private void SetRepoState(string message)
        {
            _repoStateText.Text = message;
            _repoStateText.IsVisible = message.Length > 0;
        }

        private void ShowChips(int tracked, int commits, int changes)
        {
            _stats =
            [
                $"{tracked} tracked",
                $"{commits} {(commits == 1 ? "commit" : "commits")}",
                changes == 0 ? "clean" : $"{changes} uncommitted"
            ];

            _statChips.Children.Clear();
            _statChips.Children.Add(Ui.Chip(_stats[0], AppTheme.AccentBrush));
            _statChips.Children.Add(Ui.Chip(_stats[1], AppTheme.MovedBrush));
            _statChips.Children.Add(Ui.Chip(_stats[2], changes == 0 ? AppTheme.NewBrush : AppTheme.UpdatedBrush));
        }

        private async void CommitAsync()
        {
            string message = (_commitMessageBox.Text ?? string.Empty).Trim();
            if (message.Length == 0)
            {
                SetStatus("A commit message is required.", StatusKind.Error);
                return;
            }

            bool isEmptyCommit;
            CheckVersionTool tool = CreateTool(RepoPath);
            SetReading(true);
            try
            {
                // Same folder walk the refresh does, so it must not run on the UI thread either.
                Changelist changes = await Task.Run(tool.GetChangelist);
                isEmptyCommit = changes.NewFiles.Count + changes.UpdatedFiles.Count + changes.MovedFiles.Count + changes.DeletedFiles.Count == 0;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to read repo: {ex.Message}", StatusKind.Error);
                return;
            }
            finally
            {
                SetReading(false);
            }

            // Resolve the tool's one interactive question up front, so nothing has to prompt from a
            // background thread mid-operation.
            if (isEmptyCommit
                && !await Dialogs.ConfirmAsync(this, "Empty commit", "There is no changed file. Create an empty commit anyway?"))
            {
                // Nothing else will set the line now the read is over, and leaving it on "Reading repo…" would claim work that is no longer running.
                SetStatus("Commit cancelled.", StatusKind.Pending);
                return;
            }

            _output.AutoConfirm = true;
            RunToolOperation("Commit", tool => tool.Commit(message), onCompleted: () => _commitMessageBox.Text = string.Empty);
        }

        private async void GatherAsync()
        {
            string? destination = await Dialogs.PickFolderAsync(this, "Select an empty destination folder");
            if (destination == null)
                return;

            string? subfolder = SubfolderScope;
            bool fullPaths = _fullPathsCheck.IsChecked == true;
            RunToolOperation("Gather", tool => tool.Gather(destination, subfolder, fullPaths));
        }

        private async void ArchiveAsync()
        {
            string? destination = await Dialogs.PickSaveFileAsync(this, "Save archive as", Dialogs.ZipFileType, "zip", SuggestPackFileName());
            if (destination == null)
                return;

            string? subfolder = SubfolderScope;
            bool fullPaths = _fullPathsCheck.IsChecked == true;

            // The tool refuses to overwrite, while the save picker already asked about replacing.
            if (!await EnsureOverwritableAsync(destination))
                return;

            RunToolOperation("Archive", tool => tool.Archive(destination, subfolder, fullPaths));
        }

        private async void CreateCheckpointAsync()
        {
            string? destination = await Dialogs.PickSaveFileAsync(this, "Create checkpoint as", Dialogs.ZipFileType, "zip", SuggestCheckpointFileName());
            if (destination == null)
                return;

            if (!await EnsureOverwritableAsync(destination))
                return;

            RunToolOperation("Create checkpoint", tool => tool.CreateCheckpoint(destination));
        }

        private async void RestoreCheckpointAsync()
        {
            string? source = await Dialogs.PickOpenFileAsync(this, "Select a checkpoint zip", Dialogs.ZipFileType);
            if (source == null)
                return;

            if (!await Dialogs.ConfirmAsync(
                    this,
                    "Restore checkpoint",
                    $"Restore into '{RepoPath}'?\n\nThe folder must be empty apart from the checkpoint file itself.",
                    "Restore"))
                return;

            RunToolOperation("Restore checkpoint", tool => tool.RestoreCheckpoint(source));
        }

        /// <summary>
        /// The pack commands never overwrite, so an existing destination is deleted only after the user
        /// confirms it (the file picker's own prompt does not delete anything).
        /// </summary>
        private async Task<bool> EnsureOverwritableAsync(string path)
        {
            if (!File.Exists(path))
                return true;

            if (!await Dialogs.ConfirmAsync(this, "Replace file", $"'{path}' already exists. Replace it?", "Replace"))
                return false;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Could not replace file: {ex.Message}", StatusKind.Error);
                return false;
            }
        }

        /// <summary>
        /// Run a tool call off the UI thread, with the window locked while it works.
        /// </summary>
        private async void RunToolOperation(string title, Action<CheckVersionTool> operation, Action? onCompleted = null)
        {
            if (_isBusy)
                return;

            string path = RepoPath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                SetStatus($"Folder does not exist: {path}", StatusKind.Error);
                return;
            }

            SetBusy(true, $"{title}…");
            _output.WriteLine(DrawingColor.Cyan, $"# {title}");

            CheckVersionTool tool = CreateTool(path);
            string? failure = null;

            await Task.Run(() =>
            {
                try
                {
                    operation(tool);
                }
                catch (Exception ex)
                {
                    failure = ex.Message;
                }
            });

            if (failure != null)
                _output.WriteLine(DrawingColor.Red, $"{title} failed: {failure}");

            _output.AutoConfirm = false;
            SetBusy(false, failure == null ? $"{title} finished." : $"{title} failed: {failure}", failure == null ? StatusKind.Good : StatusKind.Error);
            onCompleted?.Invoke();
            Refresh();
            await _refreshTask;
        }
        #endregion

        #region View Helpers
        private string? SubfolderScope
        {
            get
            {
                string value = (_subfolderBox.Text ?? string.Empty).Trim();
                return value.Length == 0 ? null : value;
            }
        }

        private void UpdatePackPreview()
        {
            string? subfolder = SubfolderScope;
            if (subfolder == null)
            {
                _packPreview.Text = $"Packing the whole repo: {_trackedFiles.Count} tracked {(_trackedFiles.Count == 1 ? "file" : "files")}.";
                _fullPathsCheck.IsEnabled = false;
                return;
            }

            _fullPathsCheck.IsEnabled = true;

            string prefix = subfolder.Replace('\\', '/').Trim('/');
            List<string> selected = [.. _trackedFiles.Where(p =>
                p.Length > prefix.Length
                && p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && p[prefix.Length] == '/')];

            if (selected.Count == 0)
            {
                _packPreview.Text = $"No tracked files under '{prefix}'.";
                return;
            }

            string sample = selected[0];
            string mapped = _fullPathsCheck.IsChecked == true ? sample : sample[(prefix.Length + 1)..];
            _packPreview.Text = $"Packing {selected.Count} {(selected.Count == 1 ? "file" : "files")} under '{prefix}'. Example output path: {mapped}";
        }

        private bool TryMakeRepoRelative(string absolutePath, out string relative)
        {
            relative = string.Empty;

            string root = RepoPath;
            if (string.IsNullOrWhiteSpace(root))
                return false;

            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string targetFull = Path.GetFullPath(absolutePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(rootFull, targetFull, StringComparison.OrdinalIgnoreCase))
                return true;

            string rootWithSeparator = rootFull + Path.DirectorySeparatorChar;
            if (!targetFull.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return false;

            relative = targetFull[rootWithSeparator.Length..].Replace('\\', '/');
            return true;
        }

        private string SuggestPackFileName()
        {
            string? subfolder = SubfolderScope;
            string stem = subfolder == null
                ? Path.GetFileName(RepoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                : subfolder.Replace('\\', '/').Trim('/').Replace('/', '-');

            return $"{(string.IsNullOrWhiteSpace(stem) ? "cv-archive" : stem)}.zip";
        }

        private string SuggestCheckpointFileName()
        {
            string stem = Path.GetFileName(RepoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return $"{(string.IsNullOrWhiteSpace(stem) ? "cv" : stem)}-checkpoint.zip";
        }

        private void ApplyTrackedFilter()
        {
            string filter = (_trackedFilterBox.Text ?? string.Empty).Trim();
            List<TrackedItem> shown = filter.Length == 0
                ? _trackedItems
                : [.. _trackedItems.Where(item => item.Path.Contains(filter, StringComparison.OrdinalIgnoreCase))];

            _trackedList.ItemsSource = shown;

            _trackedEmpty.IsVisible = shown.Count == 0;
            _trackedEmpty.Text = _trackedItems.Count == 0 ? "No tracked files" : "No file matches the filter";
            _trackedSummary.Text = filter.Length == 0
                ? $"{_trackedItems.Count} files"
                : $"{shown.Count} of {_trackedItems.Count} files";
        }

        private void SetBusy(bool busy, string message, StatusKind kind = StatusKind.Pending)
        {
            _isBusy = busy;
            _busyBar.IsIndeterminate = busy;
            _busyBar.IsVisible = busy;
            _busyOverlay.IsVisible = busy;
            _repoButtons.IsEnabled = !busy;
            Cursor = busy ? WaitCursor : Cursor.Default;
            SetStatus(message, kind);
        }

        private enum StatusKind
        {
            Good,
            Pending,
            Error
        }

        private void SetStatus(string message, StatusKind kind)
        {
            _statusText.Text = message;
            _statusText.Foreground = kind == StatusKind.Error ? AppTheme.DeletedBrush : AppTheme.TextDimBrush;
            _statusDot.Fill = kind switch
            {
                StatusKind.Good => AppTheme.NewBrush,
                StatusKind.Error => AppTheme.DeletedBrush,
                _ => AppTheme.UpdatedBrush
            };
        }

        private static ChangeItem Describe(FileChangeRecord record, IBrush accent)
        {
            string time = record.UpdateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return record.ChangeType switch
            {
                FileChangeRecord.FileChangeType.Moved => new ChangeItem
                {
                    Path = $"{record.Path} → {record.NewPath}",
                    Detail = time,
                    Accent = accent
                },
                FileChangeRecord.FileChangeType.Recreated => new ChangeItem
                {
                    Path = record.Path,
                    Detail = time,
                    Badge = "Recreated",
                    Accent = accent
                },
                _ => new ChangeItem
                {
                    Path = record.Path,
                    Detail = time,
                    Accent = accent
                }
            };
        }
        #endregion

        #region Subtypes
        private enum RepoState
        {
            /// <summary>Nothing to show: the folder is missing, or holds no repo yet.</summary>
            NoRepo,
            /// <summary>The repo was read successfully.</summary>
            Ready,
            /// <summary>The repo is there but could not be read.</summary>
            Failed
        }

        /// <summary>
        /// Everything the window shows, gathered off the UI thread. Deliberately holds plain data only — no control may be touched from the thread that builds it.
        /// </summary>
        private sealed class RepoSnapshot
        {
            public required RepoState State { get; init; }
            public string Message { get; init; } = string.Empty;
            public bool CanInit { get; init; }
            public Changelist? Changes { get; init; }
            public List<string> TrackedFiles { get; init; } = [];
            public List<TrackedItem> TrackedItems { get; init; } = [];
            public List<CommitItem> Commits { get; init; } = [];
            public List<string> Folders { get; init; } = [];

            public static RepoSnapshot NoRepo(string message, bool canInit)
                => new() { State = RepoState.NoRepo, Message = message, CanInit = canInit };
            public static RepoSnapshot Failed(string message)
                => new() { State = RepoState.Failed, Message = message };
        }

        /// <summary>
        /// One of the four change categories: a titled card holding a list plus its empty-state placeholder.
        /// </summary>
        private sealed class ChangeSection
        {
            public ListBox List { get; }
            public Border Card { get; }

            private readonly TextBlock _count;
            private readonly TextBlock _empty;

            public ChangeSection(string title, IBrush accent, string emptyText)
            {
                List = new ListBox { SelectionMode = SelectionMode.Single, ItemTemplate = Ui.ChangeTemplate() };
                _empty = Ui.EmptyHint(emptyText);
                _count = new TextBlock
                {
                    Classes = { "caption" },
                    VerticalAlignment = VerticalAlignment.Center
                };

                Card = Ui.Card(
                    Ui.CountHeader(title, accent, _count),
                    new Panel { Children = { List, _empty } },
                    new Thickness(0, 0, 6, 6));
            }

            public void Fill(List<ChangeItem> items)
            {
                List.ItemsSource = items;
                _count.Text = items.Count.ToString();
                _empty.IsVisible = items.Count == 0;
            }
        }
        #endregion
    }
}
