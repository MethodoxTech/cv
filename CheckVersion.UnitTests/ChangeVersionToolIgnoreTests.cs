using CheckVersion;

namespace CheckVersion.UnitTests
{
    public class CheckVersionToolIgnoreTests : IDisposable
    {
        private readonly string _tempIgnoreFile;
        private readonly CheckVersionTool _tool;

        public CheckVersionToolIgnoreTests()
        {
            // rootPath and repoControlFolderName/storage paths are irrelevant here
            _tempIgnoreFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ignore");
            _tool = new CheckVersionTool(
                repoRootPath: Directory.GetCurrentDirectory(),
                repoControlFolderName: ".cv",
                repoStorageFilePath: "dummy",
                ignoreFilename: _tempIgnoreFile
            );
        }

        [Fact]
        public void ReadIgnoreRules_FileDoesNotExist_ReturnsEmptyList()
        {
            // Ensure file is gone
            if (File.Exists(_tempIgnoreFile)) File.Delete(_tempIgnoreFile);

            List<CheckVersion.Types.IgnoreRule> rules = _tool.ReadIgnoreRules();
            Assert.NotNull(rules);
            Assert.Empty(rules);
        }

        [Fact]
        public void ReadIgnoreRules_SkipsBlankAndComments_AndParsesPatterns()
        {
            string[] lines =
            [
                "# this is a comment",
                "",
                "   ",
                "*.log",
                "!important.log",
                "#another comment",
                "data/*.csv"
            ];
            File.WriteAllLines(_tempIgnoreFile, lines);

            List<CheckVersion.Types.IgnoreRule> rules = [.. _tool.ReadIgnoreRules()];

            // Expect three rules: "*.log", "!important.log", "data/*.csv"
            Assert.Equal(3, rules.Count);

            // Check IsNegation flags in order
            Assert.False(rules[0].IsNegation);  // *.log
            Assert.True(rules[1].IsNegation);  // !important.log
            Assert.False(rules[2].IsNegation);  // data/*.csv

            // And verify they actually ignore as expected:
            Assert.True(CheckVersionTool.ShouldIgnore(rules, "errors.log"));
            Assert.False(CheckVersionTool.ShouldIgnore(rules, "important.log"));
            Assert.True(CheckVersionTool.ShouldIgnore(rules, "data/report.csv"));
            Assert.False(CheckVersionTool.ShouldIgnore(rules, "data/report.txt"));
        }

        public void Dispose()
        {
            if (File.Exists(_tempIgnoreFile))
                File.Delete(_tempIgnoreFile);
        }
    }
}
