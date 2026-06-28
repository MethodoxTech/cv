using CheckVersion.Types;

namespace CheckVersion.UnitTests
{
    public class IgnoreRuleTests
    {
        [Theory]
        // simple wildcard
        [InlineData("*.txt", "notes.txt", true)]
        [InlineData("*.txt", "docs/notes.txt", true)]
        [InlineData("*.txt", "image.png", false)]

        // negation
        [InlineData("*.log", "app.log", true)]
        [InlineData("!app.log", "app.log", false)]

        // anchored at repo root
        [InlineData("/foo/bar.cs", "foo/bar.cs", true)]
        [InlineData("/foo/bar.cs", "src/foo/bar.cs", false)]

        // path-containing patterns are repo-root relative
        [InlineData("lib/**", "lib/util/helper.cs", true)]
        [InlineData("lib/**", "src/lib/util.cs", false)]

        // use **/ explicitly when matching that folder anywhere
        [InlineData("**/lib/**", "lib/util/helper.cs", true)]
        [InlineData("**/lib/**", "src/lib/util.cs", true)]

        // single-star vs deeper path
        [InlineData("src/*.cs", "src/Program.cs", true)]
        [InlineData("src/*.cs", "src/sub/Other.cs", false)]

        // bare directory/file segment matches anywhere
        [InlineData("bin", "bin", true)]
        [InlineData("bin", "bin/file.cs", true)]
        [InlineData("bin", "src/bin/file.cs", true)]
        [InlineData("bin", "binary/file.cs", false)]

        // directory-only pattern
        [InlineData("obj/", "obj/file.cs", true)]
        [InlineData("obj/", "src/obj/file.cs", true)]
        [InlineData("obj/", "object/file.cs", false)]
        public void ShouldIgnore_VariousPatterns(string pattern, string path, bool expected)
        {
            IgnoreRule rule = new IgnoreRule(pattern);
            bool ignored = CheckVersionTool.ShouldIgnore(new[] { rule }, path);
            Assert.Equal(expected, ignored);
        }

        [Fact]
        public void ShouldIgnore_LastMatchingRuleWins()
        {
            IgnoreRule[] rules =
            [
                new IgnoreRule("*.tmp"),
                new IgnoreRule("!foo.tmp")
            ];

            Assert.False(CheckVersionTool.ShouldIgnore(rules, "foo.tmp"));
            Assert.True(CheckVersionTool.ShouldIgnore(rules, "bar.tmp"));
        }
    }
}