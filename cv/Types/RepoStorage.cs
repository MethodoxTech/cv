using System;
using System.Collections.Generic;

namespace cv.Types
{
    public class RepoStorage
    {
        #region Subtypes
        public class Commit
        {
            public List<FileChange> Changes = [];
            public string Message = string.Empty;
            public DateTime Time;
        }
        #endregion

        #region Properties
        public List<Commit> Commits = [];
        #endregion

        #region Helper Accessor
        public Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> GetLatestFiles()
        {
            Dictionary<string, (DateTime UpdateTime, DateTime CreationTime)> files = [];
            foreach (Commit commit in Commits)
            {
                foreach (FileChange fileChange in commit.Changes)
                {
                    switch (fileChange.ChangeType)
                    {
                        case FileChange.FileChangeType.New:
                        case FileChange.FileChangeType.Recreated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: new DateTime(long.Parse(fileChange.NewPath))); // Remark-cz, 20230820: Notice "NewPath" contains creation time for new/recreated files
                            break;
                        case FileChange.FileChangeType.Updated:
                            files[fileChange.Path] = (UpdateTime: fileChange.UpdateTime, CreationTime: files[fileChange.Path].CreationTime);
                            break;
                        case FileChange.FileChangeType.Deleted:
                            files.Remove(fileChange.Path);
                            break;
                        case FileChange.FileChangeType.Moved:
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
