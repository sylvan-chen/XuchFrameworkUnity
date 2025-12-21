using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
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
            BuildUtils.ShowProcessBar("xLua Generating", "Clean the old xLua wraps...", 0.1f);
            CSObjectWrapEditor.Generator.ClearAll();
            BuildUtils.ShowProcessBar("xLua Generating", "Generating xLua wrap...", 0.3f);
            CSObjectWrapEditor.Generator.GenAll();
            BuildUtils.ShowProcessBar("xLua Generating", "Refresh AssetDatabase...", 0.8f);
            AssetDatabase.Refresh();
            BuildUtils.ShowProcessBar("xLua Generating", "Done!", 1f);
#endif
        }

        public static void BuildLuaScripts()
        {
            try
            {
                BuildUtils.ShowProcessBar("Build Lua Scripts", "Loading Lua build profile...", 0f);

                var profile = Resources.Load<LuaBuildProfile>("LuaBuildProfile") ?? throw new FileNotFoundException("LuaBuildProfile not found");

                var luaScriptsDirectory = profile.LuaScriptsDirectory;
                var encryptedLuaScriptsOutputDirectory = profile.EncryptedLuaScriptsOutputDirectory;
                var ignoredDirectories = profile.IgnoredDirectoryNames;

                BuildUtils.ShowProcessBar("Build Lua Scripts", "Clean the old output directory...", 0.1f);
                if (Directory.Exists(encryptedLuaScriptsOutputDirectory))
                {
                    Directory.Delete(encryptedLuaScriptsOutputDirectory, true);
                }
                Directory.CreateDirectory(encryptedLuaScriptsOutputDirectory);

                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build Lua Scripts", "Scanning Lua scripts...", 0.2f);
                var luaFilePaths = Directory.GetFiles(luaScriptsDirectory, "*.lua", SearchOption.AllDirectories);

                var processedCount = 0;
                var totalCount = luaFilePaths.Length;

                foreach (var luaFilePath in luaFilePaths)
                {
                    processedCount++;
                    var progress = 0.2f + (processedCount / (float)totalCount) * 0.5f;
                    var fileName = Path.GetFileName(luaFilePath);
                    BuildUtils.ShowProcessBar("Build Lua Scripts", $"Encrypting Lua scripts ({processedCount}/{totalCount}): {fileName}", progress);

                    if (ignoredDirectories.Any(ignoredPath => luaFilePath.Contains(ignoredPath)))
                    {
                        continue;
                    }

                    var luaCode = GameHelper.ReadAllTextSafe(luaFilePath);
                    var encryptedLuaCode = GameHelper.Encrypt(luaCode);
                    var saveFileName = Path.GetFileNameWithoutExtension(luaFilePath) + ".bytes";
                    var encryptedFilePath = Path.Combine(encryptedLuaScriptsOutputDirectory, saveFileName);
                    GameHelper.WriteAllTextSafe(encryptedFilePath, encryptedLuaCode);
                }

                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build Lua Scripts", "Add to Addressable group...", 0.85f);
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var assetDirectory = Path.GetRelativePath(projectRoot, encryptedLuaScriptsOutputDirectory);
                BuildUtils.AddToAddressableGroup(assetDirectory, profile.AddressableGroupName, profile.AddressableLabel);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build Lua Scripts", "Done!", 1f);
            }
            catch (Exception e)
            {
                Log.Error($"[BuildLuaScripts] Failed to build Lua scripts: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}