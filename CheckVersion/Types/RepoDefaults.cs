using System.IO;

namespace CheckVersion.Types
{
    /// <summary>
    /// Well-known names used to locate a CV repo on disk.
    /// </summary>
    /// <remarks>
    /// These used to live as internal constants on <see cref="Program"/>, which made them unreachable
    /// for any host other than the CLI. They are shared here so the GUI (and tests) can construct a
    /// <see cref="CheckVersionTool"/> without duplicating magic strings.
    /// </remarks>
    public static class RepoDefaults
    {
        /// <summary>
        /// Name of the per-repo control folder, relative to the repo root.
        /// </summary>
        public const string ControlFolderName = ".cv";
        /// <summary>
        /// Name of the ignore file. One may exist at the repo root and in any subfolder.
        /// </summary>
        public const string IgnoreFilename = ".cvignore";
        /// <summary>
        /// Path of the history file, relative to the repo root.
        /// </summary>
        public static readonly string StorageFilePath = Path.Combine(ControlFolderName, "versions");
    }
}
