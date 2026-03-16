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

        public FileChangeType ChangeType { get; set; }
        public string Path { get; set; } // All paths are relative
        public string NewPath { get; set; }  // Contains creation time if it's new file, otherwise it contains new path if the file was moved
        public DateTime UpdateTime { get; set; }
        public long Size { get; set; }
    }
    public class Changelist
    {
        public List<FileChange> NewFiles { get; set; } = [];
        public List<FileChange> UpdatedFiles { get; set; } = [];
        public List<FileChange> DeletedFiles { get; set; } = [];
        public List<FileChange> MovedFiles { get; set; } = [];
    }
}
