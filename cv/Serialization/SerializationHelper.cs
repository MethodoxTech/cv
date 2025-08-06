using cv.Types;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;

namespace cv.Serialization
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
        internal static RepoStorage DeserializeFromFile(string repoStorageFilePath)
            => _deserializer.Deserialize<RepoStorage>(File.ReadAllText(repoStorageFilePath));
        internal static void SerializeToFile(RepoStorage storage, string repoStorageFilePath)
            => File.WriteAllText(repoStorageFilePath, serializer.Serialize(storage));
        #endregion
    }
}
