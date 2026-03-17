using UnityEngine;

namespace Framework.Editor
{
    [CreateAssetMenu(fileName = "LuaBuildProfile", menuName = "EdenFramework/Settings/LuaBuildProfile")]
    public sealed class LuaBuildProfile : ScriptableObject
    {
        public string LuaScriptsDirectory = "../Lua";

        public string EncryptedLuaScriptsOutputDirectory = "./BuildGenerated/EncryptedLuaScripts";

        public string[] IgnoredDirectoryNames = { "type_hints" };

        public string AddressableGroupName = "luascripts";

        public string AddressableLabel = "luascript";
    }
}