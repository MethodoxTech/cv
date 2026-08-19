using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckVersion.GUI.Views
{
    /// <summary>
    /// Small procedural helpers for the modal interactions the main window needs.
    /// </summary>
    public static class Dialogs
    {
        /// <summary>
        /// A yes/no question, built in code rather than XAML like the rest of the UI.
        /// </summary>
        public static Task<bool> ConfirmAsync(Window owner, string title, string message, string confirmText = "Yes", string cancelText = "No")
        {
            TaskCompletionSource<bool> completion = new();

            Button confirm = new() { Content = confirmText, MinWidth = 90, IsDefault = true };
            Button cancel = new() { Content = cancelText, MinWidth = 90, IsCancel = true };

            Window dialog = new()
            {
                Title = title,
                Width = 460,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 18,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, confirm }
                    }
                }
            };

            bool answer = false;
            confirm.Click += (_, _) =>
            {
                answer = true;
                dialog.Close();
            };
            cancel.Click += (_, _) => dialog.Close();
            dialog.Closed += (_, _) => completion.TrySetResult(answer);

            dialog.ShowDialog(owner);
            return completion.Task;
        }

        /// <summary>
        /// Pick an existing folder. Returns null when the user cancels or the folder has no local path.
        /// </summary>
        public static async Task<string?> PickFolderAsync(Window owner, string title, string? startAt = null)
        {
            IStorageProvider storage = owner.StorageProvider;
            FolderPickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = false,
                SuggestedStartLocation = await TryGetFolderAsync(storage, startAt)
            };

            IReadOnlyList<IStorageFolder> picked = await storage.OpenFolderPickerAsync(options);
            return picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
        }

        /// <summary>
        /// Pick an existing file. Returns null when the user cancels.
        /// </summary>
        public static async Task<string?> PickOpenFileAsync(Window owner, string title, FilePickerFileType fileType, string? startAt = null)
        {
            IStorageProvider storage = owner.StorageProvider;
            FilePickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = [fileType, FilePickerFileTypes.All],
                SuggestedStartLocation = await TryGetFolderAsync(storage, startAt)
            };

            IReadOnlyList<IStorageFile> picked = await storage.OpenFilePickerAsync(options);
            return picked.Count > 0 ? picked[0].TryGetLocalPath() : null;
        }

        /// <summary>
        /// Choose a destination file. Returns null when the user cancels.
        /// </summary>
        public static async Task<string?> PickSaveFileAsync(Window owner, string title, FilePickerFileType fileType, string defaultExtension, string? suggestedName = null, string? startAt = null)
        {
            IStorageProvider storage = owner.StorageProvider;
            FilePickerSaveOptions options = new()
            {
                Title = title,
                DefaultExtension = defaultExtension,
                SuggestedFileName = suggestedName,
                ShowOverwritePrompt = true,
                FileTypeChoices = [fileType],
                SuggestedStartLocation = await TryGetFolderAsync(storage, startAt)
            };

            IStorageFile? picked = await storage.SaveFilePickerAsync(options);
            return picked?.TryGetLocalPath();
        }

        /// <summary>
        /// Zip file type used by the archive and checkpoint pickers.
        /// </summary>
        public static readonly FilePickerFileType ZipFileType = new("Zip archive")
        {
            Patterns = ["*.zip"],
            MimeTypes = ["application/zip"]
        };

        private static async Task<IStorageFolder?> TryGetFolderAsync(IStorageProvider storage, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return await storage.TryGetFolderFromPathAsync(path);
            }
            catch (System.Exception)
            {
                // A start location is a nicety; never let it break the picker.
                return null;
            }
        }
    }
}
