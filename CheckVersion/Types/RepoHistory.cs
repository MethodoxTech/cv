using System;
using System.Collections.Generic;

namespace CheckVersion.Types
{
    public class RepoHistory
    {
        #region Subtypes
        public class Commit
        {
            public List<FileChangeRecord> Changes { get; set; } = [];
            public string Message { get; set; } = string.Empty;
            public DateTime Time { get; set; }
        }
        #endregion

        #region Properties
        public List<Commit> Commits { get; set; } = [];
        #endregion

        #region Helper Accessor
        /// <summary>
        /// Replays all serialized commits to reconstruct the current tracked file table.
        /// </summary>
        public Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> GetLatestFiles()
        {
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> files = [];
            foreach (Commit commit in Commits)
            {
                foreach (FileChangeRecord fileChange in commit.Changes)
                {
                    switch (fileChange.ChangeType)
                    {
                        case FileChangeRecord.FileChangeType.New:
                        case FileChangeRecord.FileChangeType.Recreated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: new DateTime(long.Parse(fileChange.NewPath))); // Remark-cz, 20230820: Notice "NewPath" contains creation time for new/recreated files
                            break;
                        case FileChangeRecord.FileChangeType.Updated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: files[fileChange.Path].CreationTime);
                            break;
                        case FileChangeRecord.FileChangeType.Deleted:
                            files.Remove(fileChange.Path);
                            break;
                        case FileChangeRecord.FileChangeType.Moved:
                            files[fileChange.NewPath] = (UpdateTime: fileChange.UpdateTime, CreationTime: files[fileChange.Path].CreationTime);
                            files.Remove(fileChange.Path);
                            break;
                        default:
                            break;
                    }
                }
            }
            return files;
        }
        #endregion
    }
}
