using Avalonia.Media;

namespace CheckVersion.GUI.Views
{
    /// <summary>
    /// One row in a change list. <see cref="object.ToString"/> is the plain-text form the CLI would print,
    /// which is also what the UI tests assert against.
    /// </summary>
    public sealed class ChangeItem
    {
        public required string Path { get; init; }
        public required string Detail { get; init; }
        public string? Badge { get; init; }
        public required IBrush Accent { get; init; }

        public override string ToString()
            => Badge == null
            ? $"{Path}   {Detail}"
            : $"{Path}   {Detail}   [{Badge}]";
    }

    /// <summary>
    /// One row in the tracked-file list.
    /// </summary>
    public sealed class TrackedItem
    {
        public required string Path { get; init; }
        public required bool IsMissing { get; init; }

        public override string ToString()
            => IsMissing ? $"{Path}  [Missing]" : Path;
    }

    /// <summary>
    /// One row in the history list.
    /// </summary>
    public sealed class CommitItem
    {
        public required int Index { get; init; }
        public required string Message { get; init; }
        public required string Time { get; init; }
        public required int ChangeCount { get; init; }

        public override string ToString()
            => $"{Index}.  {Time}  {Message}";
    }
}
