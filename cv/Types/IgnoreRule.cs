using System.Text.RegularExpressions;

namespace cv.Types
{
    internal class IgnoreRule
    {
        #region Properties
        private readonly Regex _regex;
        public bool IsNegation { get; }
        #endregion

        #region Construction
        public IgnoreRule(string pattern)
        {
            // !foo to unignore
            if (pattern.StartsWith('!'))
            {
                IsNegation = true;
                pattern = pattern[1..];
            }

            // if pattern starts with '/', anchor at repo root
            bool anchored = pattern.StartsWith('/');
            if (anchored) 
                pattern = pattern[1..];

            // Convert git‐style glob to regex
            _regex = new Regex(
                "^" +
                Regex.Escape(pattern)
                     // Treat "**/" specially
                     .Replace(@"\*\*/", "(@@SLUG@@/)")
                     .Replace(@"\*\*", ".*")
                     .Replace(@"\*", @"[^/]*")
                     .Replace(@"\?", @"[^/]")
                     .Replace("@@SLUG@@", ".*") +
                (anchored ? "$" : "(?:$|/)"),
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );
        }
        #endregion

        #region Methods
        public bool IsMatch(string path, string repoRoot)
        {
            // If rule was anchored, path is already relative to repoRoot
            return _regex.IsMatch(path);
        }
        #endregion
    }
}
