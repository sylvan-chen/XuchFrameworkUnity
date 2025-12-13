using UnityEngine;

namespace XuchFramework.Editor
{
    [CreateAssetMenu(fileName = "ProtoBuildProfile", menuName = "Xuch/Settings/ProtoBuildProfile")]
    public sealed class ProtoBuildProfile : ScriptableObject
    {
        [Tooltip("Proto 文件目录")]
        public string ProtosDirectory = "../Lua/csproto";

        [Tooltip("加密后的 Proto 文件输出目录")]
        public string EncryptedProtoOutputDirectory = "./BuildGenerated/EncryptedProtos";

        [Tooltip("要忽略的 Proto 文件目录名")]
        public string[] IgnoredDirectoryNames = { };

        public string AddressableGroupName = "protos";

        public string AddressableLabel = "luaproto";
    }
}