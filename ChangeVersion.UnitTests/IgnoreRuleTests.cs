using cv.Types;
using cv;

namespace ChangeVersion.UnitTests
{
    public class IgnoreRuleTests
    {
        [Theory]
        // simple wildcard
        [InlineData("*.txt", "notes.txt", true)]
        [InlineData("*.txt", "image.png", false)]
        // negation
        [InlineData("*.log", "app.log", true)]
        [InlineData("!app.log", "app.log", false)]
        // anchored at repo root
        [InlineData("/foo/bar.cs", "foo/bar.cs", true)]
        [InlineData("/foo/bar.cs", "src/foo/bar.cs", false)]
        // double-star
        [InlineData("lib/**", "lib/util/helper.cs", true)]
        [InlineData("lib/**", "src/lib/util.cs", false)]
        // single-star vs deeper path
        [InlineData("src/*.cs", "src/Program.cs", true)]
        [InlineData("src/*.cs", "src/sub/Other.cs", false)]
        public void ShouldIgnore_VariousPatterns(string pattern, string path, bool expected)
        {
            IgnoreRule rule = new IgnoreRule(pattern);
            bool ignored = ChangeVersionTool.ShouldIgnore(new[] { rule }, path);
            Assert.Equal(expected, ignored);
        }

        [Fact]
        public void ShouldIgnore_LastMatchingRuleWins()
        {
            // First says ignore all .tmp, then un-ignore foo.tmp
            IgnoreRule[] rules =
            [
                new IgnoreRule("*.tmp"),
                new IgnoreRule("!foo.tmp")
            ];

            Assert.False(ChangeVersionTool.ShouldIgnore(rules, "foo.tmp"));
            Assert.True(ChangeVersionTool.ShouldIgnore(rules, "bar.tmp"));
        }
    }
}