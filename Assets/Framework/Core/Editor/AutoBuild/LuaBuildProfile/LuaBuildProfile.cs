using UnityEngine;

namespace XuchFramework.Editor
{
    [CreateAssetMenu(fileName = "LuaBuildProfile", menuName = "XuchFramework/Settings/LuaBuildProfile")]
    public sealed class LuaBuildProfile : ScriptableObject
    {
        public string LuaScriptsDirectory = "../Lua";

        public string EncryptedLuaScriptsOutputDirectory = "./BuildGenerated/EncryptedLuaScripts";

        public string[] IgnoredDirectoryNames = { "type_hints" };

        public string AddressableGroupName = "luascripts";

        public string AddressableLabel = "luascript";
    }
}