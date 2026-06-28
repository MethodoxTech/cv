using CheckVersion.Types;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;

namespace CheckVersion.Serialization
{
    public static class SerializationHelper
    {
        #region Configurations
        private readonly static IDeserializer _deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        private readonly static ISerializer serializer = new StaticSerializerBuilder(new YamlStaticContext())
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .EnsureRoundtrip()
            .Build();
        #endregion

        #region Methods
        internal static RepoHistory DeserializeFromFile(string repoStorageFilePath)
            => File.Exists(repoStorageFilePath)
            ? _deserializer.Deserialize<RepoHistory>(File.ReadAllText(repoStorageFilePath))
            : new();
        internal static void SerializeToFile(RepoHistory storage, string repoStorageFilePath)
            => File.WriteAllText(repoStorageFilePath, serializer.Serialize(storage));
        #endregion
    }
}
