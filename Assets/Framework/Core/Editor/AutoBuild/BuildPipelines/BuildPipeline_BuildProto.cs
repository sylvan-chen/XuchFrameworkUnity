using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_BuildProto
    {
        public static void Run(BuildConfig buildConfig)
        {
            if (buildConfig.BuildProto)
            {
                BuildProtoFiles();
            }
        }

        public static void BuildProtoFiles()
        {
            try
            {
                BuildUtils.ShowProcessBar("Build proto files", "Loading proto build profile...", 0f);

                var profile = Resources.Load<ProtoBuildProfile>("ProtoBuildProfile")
                              ?? throw new FileNotFoundException("ProtoBuildProfile not found");

                var protosDirectory = profile.ProtosDirectory;
                var encryptedProtoOutputDirectory = profile.EncryptedProtoOutputDirectory;
                var ignoredDirectories = profile.IgnoredDirectoryNames;

                BuildUtils.ShowProcessBar("Build proto files", "Clean the old output directory...", 0.1f);
                if (Directory.Exists(encryptedProtoOutputDirectory))
                {
                    Directory.Delete(encryptedProtoOutputDirectory, true);
                }
                Directory.CreateDirectory(encryptedProtoOutputDirectory);

                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build proto files", "Scanning Lua scripts...", 0.2f);
                var protoFilePaths = Directory.GetFiles(protosDirectory, "*.proto", SearchOption.AllDirectories);

                var processedCount = 0;
                var totalCount = protoFilePaths.Length;

                foreach (var protoPath in protoFilePaths)
                {
                    processedCount++;
                    var progress = 0.2f + (processedCount / (float)totalCount) * 0.5f;
                    var fileName = Path.GetFileName(protoPath);
                    BuildUtils.ShowProcessBar("Build proto files", $"Encrypting proto files ({processedCount}/{totalCount}): {fileName}", progress);

                    if (ignoredDirectories.Any(ignoredPath => protoPath.Contains(ignoredPath)))
                    {
                        continue;
                    }

                    var protoCode = FileHelper.ReadAllTextSafe(protoPath);
                    var encryptedProtoCode = EncryptionHelper.Encrypt(protoCode);
                    var saveFileName = Path.GetFileNameWithoutExtension(protoPath) + ".bytes";
                    var encryptedFilePath = Path.Combine(encryptedProtoOutputDirectory, saveFileName);
                    FileHelper.WriteAllTextSafe(encryptedFilePath, encryptedProtoCode);
                }

                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build proto files", "Add to Addressable group...", 0.85f);
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var assetDirectory = Path.GetRelativePath(projectRoot, encryptedProtoOutputDirectory);
                BuildUtils.AddToAddressableGroup(assetDirectory, profile.AddressableGroupName, profile.AddressableLabel);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                BuildUtils.ShowProcessBar("Build proto files", "Done!", 1f);
            }
            catch (Exception e)
            {
                Log.Error($"[BuildProtoFiles] Failed to build proto files: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}