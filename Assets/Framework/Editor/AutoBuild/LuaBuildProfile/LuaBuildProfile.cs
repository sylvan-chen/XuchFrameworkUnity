using UnityEngine;

namespace Xuch.Editor
{
    [CreateAssetMenu(fileName = "LuaBuildProfile", menuName = "DigiEden/Settings/LuaBuildProfile")]
    public sealed class LuaBuildProfile : ScriptableObject
    {
        [Tooltip("Lua 脚本目录")]
        public string LuaScriptsDirectory = "../Lua";

        [Tooltip("加密后的 Lua 脚本输出目录")]
        public string EncryptedLuaSciptsOutputDirectory = "./BuildGenerated/EncryptedLuaScripts";

        [Tooltip("要忽略的 Lua 脚本目录名")]
        public string[] IgnoredDirectoryNames = { "type_hints" };

        public string AddressableGroupName = "luascripts";

        public string AddressableLabel = "luascript";
    }
}