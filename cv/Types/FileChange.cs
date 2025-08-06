using System;
using System.Collections.Generic;

namespace cv.Types
{
    public class FileChange
    {
        public enum FileChangeType
        {
            New,    // Completely new
            Updated, // Same path, different update time
            Deleted, // No longer exist at path
            Moved,   // Save file save name save creation date somewhere else
            Recreated, // Deleted then recreated with the same path
        }

        public FileChangeType ChangeType;
        public string Path; // All paths are relative
        public string NewPath;  // Contains creation time if it's new file, otherwise it contains new path if the file was moved
        public DateTime UpdateTime;
        public long Size;
    }
    public class Changelist
    {
        public List<FileChange> NewFiles = [];
        public List<FileChange> UpdatedFiles = [];
        public List<FileChange> DeletedFiles = [];
        public List<FileChange> MovedFiles = [];
    }
}
