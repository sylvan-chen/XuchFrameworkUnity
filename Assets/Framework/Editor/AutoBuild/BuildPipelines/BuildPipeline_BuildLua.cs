using System.IO;
using System.Linq;
using DigiEden.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    public static class BuildPipeline_BuildLua
    {
        public static void Run(BuildConfig buildConfig)
        {
            if (buildConfig.BuildLua)
            {
                XLuaGen();
                BuildLuaScripts();
            }
        }

        private static void XLuaGen()
        {
#if XLUA
            BuildUtils.ShowProcessBar("xLua Generating", "清理旧 xLua Wrap...", 0.1f);
            CSObjectWrapEditor.Generator.ClearAll();
            BuildUtils.ShowProcessBar("xLua Generating", "生成 xLua Wrap...", 0.3f);
            CSObjectWrapEditor.Generator.GenAll();
            BuildUtils.ShowProcessBar("xLua Generating", "刷新资源数据库...", 0.8f);
            AssetDatabase.Refresh();
            BuildUtils.ShowProcessBar("xLua Generating", "完成!", 1f);
#endif
        }

        public static void BuildLuaScripts()
        {
            BuildUtils.ShowProcessBar("构建 Lua 脚本", "加载配置...", 0f);

            var profile = Resources.Load<LuaBuildProfile>("LuaBuildProfile") ?? throw new FileNotFoundException("LuaBuildProfile not found");

            var luaScriptsDirectory = profile.LuaScriptsDirectory;
            var encryptedLuaScriptsOutputDirectory = profile.EncryptedLuaSciptsOutputDirectory;
            var ignoredDirectories = profile.IgnoredDirectoryNames;

            BuildUtils.ShowProcessBar("构建 Lua 脚本", "清理旧输出目录...", 0.1f);
            if (Directory.Exists(encryptedLuaScriptsOutputDirectory))
            {
                Directory.Delete(encryptedLuaScriptsOutputDirectory, true);
            }
            Directory.CreateDirectory(encryptedLuaScriptsOutputDirectory);

            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("构建 Lua 脚本", "扫描所有 Lua 文件...", 0.2f);
            var luaFilePaths = Directory.GetFiles(luaScriptsDirectory, "*.lua", SearchOption.AllDirectories);

            var processedCount = 0;
            var totalCount = luaFilePaths.Length;

            foreach (var luaFilePath in luaFilePaths)
            {
                processedCount++;
                var progress = 0.2f + (processedCount / (float)totalCount) * 0.5f;
                var fileName = Path.GetFileName(luaFilePath);
                BuildUtils.ShowProcessBar("构建 Lua 脚本", $"加密 Lua 脚本 ({processedCount}/{totalCount}): {fileName}", progress);

                if (ignoredDirectories.Any(ignoredPath => luaFilePath.Contains(ignoredPath)))
                {
                    continue;
                }

                var luaCode = FileHelper.ReadAllTextSafe(luaFilePath);
                var encryptedLuaCode = EncryptionHelper.Encrypt(luaCode);
                var saveFileName = Path.GetFileNameWithoutExtension(luaFilePath) + ".bytes";
                var encryptedFilePath = Path.Combine(encryptedLuaScriptsOutputDirectory, saveFileName);
                FileHelper.WriteAllTextSafe(encryptedFilePath, encryptedLuaCode);
            }

            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("构建 Lua 脚本", "添加到 Addressable 组...", 0.85f);
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var assetDirectory = Path.GetRelativePath(projectRoot, encryptedLuaScriptsOutputDirectory);
            BuildUtils.AddToAddressableGroup(assetDirectory, profile.AddressableGroupName, profile.AddressableLabel);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("构建 Lua 脚本", "完成！", 1f);
        }
    }
}