using System.Text.RegularExpressions;

namespace cv.Types
{
    public class IgnoreRule
    {
        public string Pattern { get; }
        public bool IsNegation { get; }

        private readonly Regex _regex;

        public IgnoreRule(string pattern)
        {
            if (pattern.StartsWith('!'))
            {
                IsNegation = true;
                pattern = pattern[1..];
            }

            pattern = pattern.Replace('\\', '/').Trim();

            bool anchored = pattern.StartsWith('/');
            if (anchored)
                pattern = pattern[1..];

            bool directoryOnly = pattern.EndsWith('/');
            if (directoryOnly)
                pattern = pattern[..^1];

            bool hasSlash = pattern.Contains('/');

            string regexBody = Regex.Escape(pattern)
                .Replace(@"\*\*/", @"(.*/)?")
                .Replace(@"\*\*", @".*")
                .Replace(@"\*", @"[^/]*")
                .Replace(@"\?", @"[^/]");

            string prefix;
            if (anchored)
            {
                // Must match from repo root
                prefix = "^";
            }
            else if (hasSlash)
            {
                // Unanchored pattern containing slash can match anywhere
                prefix = @"^(?:.*/)?";
            }
            else
            {
                // Bare name like "bin" or "obj" matches any path segment
                prefix = @"^(?:|.*/)";
            }

            string suffix = directoryOnly
                ? @"(?:/.*)?$"   // directory and everything under it
                : hasSlash || anchored
                    ? @"(?:$|/.*$)" // exact path or children if directory
                    : @"(?:$|/.*$)"; // bare segment or anything under it

            _regex = new Regex(
                prefix + regexBody + suffix,
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            Pattern = pattern;
        }
        public bool IsMatch(string path, string repoRoot)
        {
            path = path.Replace('\\', '/').TrimStart('/');
            return _regex.IsMatch(path);
        }
    }
}
