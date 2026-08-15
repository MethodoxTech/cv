using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace CheckVersion.GUI.Views
{
    /// <summary>
    /// Construction helpers for the recurring pieces of the UI. These are the code-behind equivalent of the
    /// small reusable templates a XAML project would keep in a resource dictionary.
    /// </summary>
    public static class Ui
    {
        #region Text
        public static TextBlock Heading(string text)
            => new() { Text = text, Classes = { "h2" } };

        public static TextBlock Title(string text)
            => new() { Text = text, Classes = { "h1" } };

        public static TextBlock Caption(string text)
            => new()
            {
                Text = text,
                Classes = { "caption" },
                TextWrapping = TextWrapping.Wrap
            };
        #endregion

        #region Surfaces
        /// <summary>
        /// A titled panel. <paramref name="accent"/> tints the title and its leading marker, which is how the
        /// four change categories stay distinguishable at a glance.
        /// </summary>
        public static Border Card(Control header, Control content, Thickness? margin = null)
            => new()
            {
                Background = AppTheme.SurfaceBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(AppTheme.CornerLarge),
                Margin = margin ?? new Thickness(0),
                Child = new DockPanel
                {
                    Children =
                    {
                        Docked(new Border
                        {
                            Padding = new Thickness(14, 10),
                            BorderBrush = AppTheme.LineBrush,
                            BorderThickness = new Thickness(0, 0, 0, 1),
                            Child = header
                        }, Dock.Top),
                        new Border { Padding = new Thickness(6), Child = content }
                    }
                }
            };

        /// <summary>
        /// Card header showing a colored marker, a name, and a count.
        /// </summary>
        public static Control CountHeader(string title, IBrush accent, TextBlock countText)
            => new DockPanel
            {
                Children =
                {
                    Docked(countText, Dock.Right),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new Border
                            {
                                Width = 3,
                                Height = 14,
                                CornerRadius = new CornerRadius(2),
                                Background = accent,
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            Heading(title)
                        }
                    }
                }
            };

        /// <summary>
        /// A small rounded stat pill, e.g. "6 tracked".
        /// </summary>
        public static Border Chip(string text, IBrush accent)
            => new()
            {
                Background = AppTheme.SurfaceRaisedBrush,
                BorderBrush = AppTheme.LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(9, 3),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = accent,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = text,
                            FontSize = AppTheme.FontSmall,
                            Foreground = AppTheme.TextDimBrush,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

        /// <summary>
        /// Placeholder shown in place of an empty list, so a card never reads as broken.
        /// </summary>
        public static TextBlock EmptyHint(string text)
            => new()
            {
                Text = text,
                FontSize = AppTheme.FontSmall,
                Foreground = AppTheme.TextFaintBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 18)
            };
        #endregion

        #region Buttons
        public static Button Action(string text, string? iconPath = null, bool accent = false, double minWidth = 0)
        {
            Button button = new()
            {
                Content = iconPath == null ? text : WithIcon(iconPath, text, accent ? Brushes.White : AppTheme.TextDimBrush),
                MinWidth = minWidth
            };

            if (accent)
                button.Classes.Add("accent");
            return button;
        }

        public static Button Quiet(string text, string? iconPath = null)
        {
            Button button = new()
            {
                Content = iconPath == null ? text : WithIcon(iconPath, text, AppTheme.TextDimBrush)
            };
            button.Classes.Add("quiet");
            return button;
        }

        private static Control WithIcon(string iconPath, string text, IBrush iconBrush)
            => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 7,
                Children =
                {
                    new Path
                    {
                        Data = Geometry.Parse(iconPath),
                        Stroke = iconBrush,
                        StrokeThickness = 1.4,
                        StrokeLineCap = PenLineCap.Round,
                        StrokeJoin = PenLineJoin.Round,
                        Width = 14,
                        Height = 14,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
                }
            };
        #endregion

        #region Icons
        // Simple 16x16 line glyphs; drawn rather than pulled from an icon font so the app has no font dependency.
        public const string IconFolder = "M2,5 L6,5 L7.5,7 L14,7 L14,13 L2,13 Z";
        public const string IconOpen = "M3,4 L7,4 L8.5,6 L13,6 M2.5,13 L4.5,8 L14.5,8 L12.5,13 Z";
        public const string IconRefresh = "M13,8 A5,5 0 1 1 11.2,4.2 M13,2 L13,4.6 L10.4,4.6";
        public const string IconPlus = "M8,3.5 L8,12.5 M3.5,8 L12.5,8";
        public const string IconCommit = "M2,8 L5.5,8 M10.5,8 L14,8 M8,5 A3,3 0 1 1 8,11 A3,3 0 1 1 8,5";
        public const string IconFolderOut = "M2,4 L6,4 L7.5,6 L14,6 L14,13 L2,13 Z M8,8 L8,11 M6.5,9.5 L8,11 L9.5,9.5";
        public const string IconZip = "M4,2 L12,2 L12,14 L4,14 Z M7.5,2 L7.5,4 M8.5,4 L8.5,6 M7.5,6 L7.5,8 M8.5,8 L8.5,10";
        public const string IconCheckpoint = "M8,2 L13.5,5 L13.5,11 L8,14 L2.5,11 L2.5,5 Z M5.5,8 L7.3,9.8 L10.5,6.6";
        public const string IconRestore = "M3,8 A5,5 0 1 0 4.8,4.2 M3,2 L3,4.6 L5.6,4.6";
        public const string IconClear = "M3,5 L13,5 M6.5,5 L6.5,3.5 L9.5,3.5 L9.5,5 M4.5,5 L5,13 L11,13 L11.5,5";
        #endregion

        #region Templates
        /// <summary>
        /// Change row: colored dot, monospace path, then the timestamp and any badge on the right.
        /// </summary>
        public static IDataTemplate ChangeTemplate()
            => new FuncDataTemplate<ChangeItem>((item, _) =>
            {
                if (item == null)
                    return new TextBlock();

                StackPanel trailing = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (item.Badge != null)
                    trailing.Children.Add(new Border
                    {
                        Background = AppTheme.SurfaceRaisedBrush,
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = item.Badge,
                            FontSize = 10,
                            Foreground = item.Accent
                        }
                    });

                trailing.Children.Add(new TextBlock
                {
                    Text = item.Detail,
                    FontSize = AppTheme.FontSmall,
                    Foreground = AppTheme.TextFaintBrush,
                    VerticalAlignment = VerticalAlignment.Center
                });

                return new DockPanel
                {
                    Children =
                    {
                        Docked(trailing, Dock.Right),
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 9,
                            Children =
                            {
                                new Ellipse
                                {
                                    Width = 6,
                                    Height = 6,
                                    Fill = item.Accent,
                                    VerticalAlignment = VerticalAlignment.Center
                                },
                                new TextBlock
                                {
                                    Text = item.Path,
                                    FontFamily = AppTheme.MonoFont,
                                    FontSize = AppTheme.FontSmall,
                                    TextTrimming = TextTrimming.CharacterEllipsis,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            }
                        }
                    }
                };
            });

        public static IDataTemplate TrackedTemplate()
            => new FuncDataTemplate<TrackedItem>((item, _) =>
            {
                if (item == null)
                    return new TextBlock();

                DockPanel row = new();

                if (item.IsMissing)
                    row.Children.Add(Docked(new Border
                    {
                        Background = AppTheme.SurfaceRaisedBrush,
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "Missing",
                            FontSize = 10,
                            Foreground = AppTheme.DeletedBrush
                        }
                    }, Dock.Right));

                row.Children.Add(new TextBlock
                {
                    Text = item.Path,
                    FontFamily = AppTheme.MonoFont,
                    FontSize = AppTheme.FontSmall,
                    Foreground = item.IsMissing ? AppTheme.TextDimBrush : AppTheme.TextBrush,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                });

                return row;
            });

        public static IDataTemplate CommitTemplate()
            => new FuncDataTemplate<CommitItem>((item, _) =>
            {
                if (item == null)
                    return new TextBlock();

                return new DockPanel
                {
                    Children =
                    {
                        Docked(new TextBlock
                        {
                            Text = item.Time,
                            FontSize = AppTheme.FontSmall,
                            Foreground = AppTheme.TextFaintBrush,
                            VerticalAlignment = VerticalAlignment.Center
                        }, Dock.Right),
                        Docked(new TextBlock
                        {
                            Text = $"#{item.Index}",
                            FontFamily = AppTheme.MonoFont,
                            FontSize = AppTheme.FontSmall,
                            Foreground = AppTheme.AccentBrush,
                            MinWidth = 34,
                            VerticalAlignment = VerticalAlignment.Center
                        }, Dock.Left),
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = item.Message,
                                    TextTrimming = TextTrimming.CharacterEllipsis,
                                    VerticalAlignment = VerticalAlignment.Center
                                },
                                new TextBlock
                                {
                                    Text = $"{item.ChangeCount} {(item.ChangeCount == 1 ? "file" : "files")}",
                                    FontSize = AppTheme.FontSmall,
                                    Foreground = AppTheme.TextFaintBrush,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            }
                        }
                    }
                };
            });
        #endregion

        #region Layout
        public static T Docked<T>(T control, Dock dock) where T : Control
        {
            DockPanel.SetDock(control, dock);
            return control;
        }
        #endregion
    }
}
