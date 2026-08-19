using CheckVersion.Types;
using System.Collections.Generic;

namespace CheckVersion.UnitTests
{
    /// <summary>
    /// `.cvignore` files placed in subfolders, matching git's nested-ignore semantics.
    /// </summary>
    public class NestedIgnoreTests
    {
        [Fact]
        public void NestedIgnore_AppliesOnlyWithinItsOwnFolder()
        {
            using TestRepo repo = new();

            repo.WriteFile("keep.log", "root log");
            repo.WriteFile("assets/keep.txt", "kept");
            repo.WriteFile("assets/scratch.log", "scratch");
            repo.WriteFile("assets/.cvignore", "*.log\n");

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            List<string> tracked = repo.Tool.GetTrackedFiles();
            Assert.Contains("keep.log", tracked);           // root file untouched by the nested rule
            Assert.Contains("assets/keep.txt", tracked);
            Assert.DoesNotContain("assets/scratch.log", tracked);
        }

        [Fact]
        public void NestedIgnore_CanReIncludeWhatTheRootIgnored()
        {
            using TestRepo repo = new();

            repo.WriteFile(".cvignore", "*.bin\n");
            repo.WriteFile("data/a.bin", "a");
            repo.WriteFile("shipped/b.bin", "b");
            repo.WriteFile("shipped/.cvignore", "!*.bin\n");

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            List<string> tracked = repo.Tool.GetTrackedFiles();
            Assert.DoesNotContain("data/a.bin", tracked);
            Assert.Contains("shipped/b.bin", tracked);
        }

        [Fact]
        public void NestedIgnore_AnchoredPatternIsRelativeToItsOwnFolder()
        {
            using TestRepo repo = new();

            repo.WriteFile("assets/.cvignore", "/raw\n");
            repo.WriteFile("assets/raw/big.psd", "psd");
            repo.WriteFile("assets/textures/raw/small.png", "png"); // not at the anchor point
            repo.WriteFile("raw/root.txt", "root");                 // outside the nested scope entirely

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            List<string> tracked = repo.Tool.GetTrackedFiles();
            Assert.DoesNotContain("assets/raw/big.psd", tracked);
            Assert.Contains("assets/textures/raw/small.png", tracked);
            Assert.Contains("raw/root.txt", tracked);
        }

        [Fact]
        public void NestedIgnore_IgnoresWholeSubtreeAndDoesNotDescend()
        {
            using TestRepo repo = new();

            repo.WriteFile("game/.cvignore", "build\n");
            repo.WriteFile("game/src/main.cs", "code");
            repo.WriteFile("game/build/out.exe", "exe");
            repo.WriteFile("game/build/logs/a.txt", "log");

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            List<string> tracked = repo.Tool.GetTrackedFiles();
            Assert.Contains("game/src/main.cs", tracked);
            Assert.DoesNotContain("game/build/out.exe", tracked);
            Assert.DoesNotContain("game/build/logs/a.txt", tracked);
        }

        [Fact]
        public void NestedIgnore_InsideAFolderTheRootExcluded_IsNeverReached()
        {
            using TestRepo repo = new();

            repo.WriteFile(".cvignore", "vendor\n");
            repo.WriteFile("vendor/.cvignore", "!*\n");
            repo.WriteFile("vendor/lib.dll", "dll");

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            // Same as git: you cannot re-include out of a folder that was excluded higher up.
            Assert.DoesNotContain("vendor/lib.dll", repo.Tool.GetTrackedFiles());
        }

        [Fact]
        public void NestedIgnore_DeeperFileOverridesShallowerOne()
        {
            using TestRepo repo = new();

            repo.WriteFile("a/.cvignore", "*.tmp\n");
            repo.WriteFile("a/one.tmp", "one");
            repo.WriteFile("a/b/.cvignore", "!*.tmp\n");
            repo.WriteFile("a/b/two.tmp", "two");

            repo.Tool.Init();
            repo.Tool.Commit("initial");

            List<string> tracked = repo.Tool.GetTrackedFiles();
            Assert.DoesNotContain("a/one.tmp", tracked);
            Assert.Contains("a/b/two.tmp", tracked);
        }

        [Fact]
        public void IgnoreScope_ScopesPathsToItsOwnFolder()
        {
            IgnoreScope scope = new("assets/textures", CheckVersionTool.ParseIgnoreRules(["*.png"]));

            Assert.True(scope.TryGetScopedPath("assets/textures/tree.png", out string scoped));
            Assert.Equal("tree.png", scoped);

            Assert.False(scope.TryGetScopedPath("assets/other/tree.png", out _));
            Assert.False(scope.TryGetScopedPath("assets/textures", out _));
        }

        [Fact]
        public void IgnoreContext_LastMatchWinsAcrossLayers()
        {
            IgnoreContext context = IgnoreContext
                .FromRootRules(CheckVersionTool.ParseIgnoreRules(["*.bin"]))
                .Push(new IgnoreScope("shipped", CheckVersionTool.ParseIgnoreRules(["!*.bin"])));

            Assert.True(context.ShouldIgnore("data/a.bin"));
            Assert.False(context.ShouldIgnore("shipped/b.bin"));
        }

        [Fact]
        public void IgnoreContext_WithNoRules_IgnoresNothing()
            => Assert.False(IgnoreContext.Empty.ShouldIgnore("anything/at/all.txt"));

        [Fact]
        public void ParseIgnoreRules_SkipsBlanksAndComments()
        {
            List<IgnoreRule> rules = CheckVersionTool.ParseIgnoreRules(["# comment", "", "   ", "*.log", "!keep.log"]);

            Assert.Equal(2, rules.Count);
            Assert.False(rules[0].IsNegation);
            Assert.True(rules[1].IsNegation);
        }
    }
}
