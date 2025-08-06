using cv.Types;
using YamlDotNet.Serialization;

namespace cv.Serialization
{
    [YamlStaticContext]
    [YamlSerializable(typeof(RepoStorage))]
    [YamlSerializable(typeof(RepoStorage.Commit))]
    [YamlSerializable(typeof(FileChange))]
    [YamlSerializable(typeof(Changelist))]
    [YamlSerializable(typeof(FileChange.FileChangeType))]
    public partial class YamlStaticContext : YamlDotNet.Serialization.StaticContext
    {
    }
}
