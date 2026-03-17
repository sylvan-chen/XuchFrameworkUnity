using UnityEngine;

namespace Framework.Editor
{
    [CreateAssetMenu(fileName = "ProtoBuildProfile", menuName = "EdenFramework/Settings/ProtoBuildProfile")]
    public sealed class ProtoBuildProfile : ScriptableObject
    {
        public string ProtosDirectory = "../Proto";

        public string EncryptedProtoOutputDirectory = "./BuildGenerated/EncryptedProtos";

        public string[] IgnoredDirectoryNames = { };

        public string AddressableGroupName = "protos";

        public string AddressableLabel = "luaproto";
    }
}