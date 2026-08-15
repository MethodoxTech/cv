using System;
using System.Collections.Generic;

namespace CheckVersion.Types
{
    /// <summary>
    /// The rules of a single ignore file, together with the folder that file lives in.
    /// </summary>
    /// <remarks>
    /// Patterns inside an ignore file are interpreted relative to that file's own folder, exactly like
    /// git does for nested `.gitignore` files. A rule written in `assets/.cvignore` as `/raw` therefore
    /// means `assets/raw`, not a repo-root `raw`.
    /// </remarks>
    public sealed class IgnoreScope
    {
        /// <summary>
        /// Folder holding the ignore file, relative to the repo root, using forward slashes and no
        /// trailing slash. Empty for the repo root itself.
        /// </summary>
        public string BaseDirectory { get; }
        public IReadOnlyList<IgnoreRule> Rules { get; }

        public IgnoreScope(string baseDirectory, IReadOnlyList<IgnoreRule> rules)
        {
            BaseDirectory = NormalizeDirectory(baseDirectory);
            Rules = rules;
        }

        /// <summary>
        /// Translate a repo-root-relative path into one relative to this scope's folder.
        /// Returns false when the path lies outside the scope, in which case the scope does not apply.
        /// </summary>
        public bool TryGetScopedPath(string repoRelativePath, out string scopedPath)
        {
            string path = repoRelativePath.Replace('\\', '/').TrimStart('/');

            if (BaseDirectory.Length == 0)
            {
                scopedPath = path;
                return true;
            }

            if (path.Length > BaseDirectory.Length
                && path.StartsWith(BaseDirectory, StringComparison.OrdinalIgnoreCase)
                && path[BaseDirectory.Length] == '/')
            {
                scopedPath = path[(BaseDirectory.Length + 1)..];
                return true;
            }

            scopedPath = string.Empty;
            return false;
        }

        internal static string NormalizeDirectory(string directory)
            => directory.Replace('\\', '/').Trim('/');
    }

    /// <summary>
    /// The stack of ignore scopes that applies at some point of the folder walk, ordered outermost first.
    /// </summary>
    /// <remarks>
    /// Resolution follows git's precedence: within one ignore file the last matching pattern wins, and a
    /// deeper ignore file overrides a shallower one (including re-including a file the root ignored).
    /// Both fall out of evaluating the scopes in outermost-to-innermost order and keeping the last match.
    ///
    /// The one git behavior we intentionally keep is that a file inside an excluded folder cannot be
    /// re-included, because the walk never descends into an ignored folder in the first place.
    /// </remarks>
    public sealed class IgnoreContext
    {
        public static readonly IgnoreContext Empty = new([]);

        private readonly IReadOnlyList<IgnoreScope> _scopes;

        public IgnoreContext(IReadOnlyList<IgnoreScope> scopes)
            => _scopes = scopes;

        public IReadOnlyList<IgnoreScope> Scopes => _scopes;

        /// <summary>
        /// Produce a new context with <paramref name="scope"/> layered on top of this one.
        /// </summary>
        public IgnoreContext Push(IgnoreScope scope)
            => new([.. _scopes, scope]);

        /// <summary>
        /// Decide whether a repo-root-relative path is ignored under the currently layered rules.
        /// </summary>
        public bool ShouldIgnore(string repoRelativePath)
        {
            bool? ignored = null;

            // Outermost first, so a deeper ignore file naturally has the final say.
            foreach (IgnoreScope scope in _scopes)
            {
                if (!scope.TryGetScopedPath(repoRelativePath, out string scopedPath))
                    continue;

                foreach (IgnoreRule rule in scope.Rules)
                {
                    if (!rule.IsMatch(scopedPath, string.Empty))
                        continue;

                    // Last matching rule wins.
                    ignored = !rule.IsNegation;
                }
            }

            return ignored.GetValueOrDefault(false);
        }

        /// <summary>
        /// Convenience factory for a context holding only repo-root rules.
        /// </summary>
        public static IgnoreContext FromRootRules(IReadOnlyList<IgnoreRule> rules)
            => rules.Count == 0
            ? Empty
            : new([new IgnoreScope(string.Empty, rules)]);
    }
}
