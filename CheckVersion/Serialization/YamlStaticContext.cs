using CheckVersion.Types;
using YamlDotNet.Serialization;

namespace CheckVersion.Serialization
{
    [YamlStaticContext]
    [YamlSerializable(typeof(RepoHistory))]
    [YamlSerializable(typeof(RepoHistory.Commit))]
    [YamlSerializable(typeof(FileChangeRecord))]
    [YamlSerializable(typeof(Changelist))]
    [YamlSerializable(typeof(FileChangeRecord.FileChangeType))]
    public partial class YamlStaticContext : YamlDotNet.Serialization.StaticContext
    {
    }
}
