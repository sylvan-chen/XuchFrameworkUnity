using UnityEditor;
using UnityEngine;

namespace XuchFramework.Editor
{
    public enum PlayerCompressionType
    {
        LZ4 = 0,
        LZ4HC = 1,
    }

    [CreateAssetMenu(fileName = "BuildConfig", menuName = "Xuch/Build Config")]
    public class BuildConfig : ScriptableObject
    {
        public BuildTarget BuildTarget;
        public string OutputDirectory;
        public string BuildName;

        public string AppIdentifier;
        public string AppVersion;
        public int BundleVersionCode; // For Android
        public string BundleNumber;   // For iOS

        public string CompanyName;
        public string ProductName;

        public bool BuildLua = true;

        public bool BuildAddressables = true;
        public bool AddressablesCleanBuild = true;
        public string AddressablesActiveProfile = "Default";

        public bool BuildProto = true;

        public string MacroDefinitions;

        public bool DevelopmentBuild = false;
        public bool AutoconnectProfiler = false;
        public bool DeepProfilingSurpport = false;
        public bool ScriptDebugging = false;

        public PlayerCompressionType PlayerCompression;

        // For Andoird
        public int DebugSymbols;
        public bool UseCustomKeystore = false;
        public string KeystoreName;
        public string KeystorePass;
        public string KeyaliasName;
        public string KeyaliasPass;
        public bool MinifyRelease = true;
        public bool MinifyDebug = false;
        public bool SplitApplicationBinary;
        public bool BuildAppBundle;
    }
}