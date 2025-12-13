using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_BuildProto
    {
        public static void Run(BuildConfig buildConfig)
        {
            if (!buildConfig.BuildProto)
                return;

            BuildUtils.ShowProcessBar("构建 Proto", "加载配置...", 0f);

            var profile = Resources.Load<ProtoBuildProfile>("ProtoBuildProfile") ?? throw new FileNotFoundException("ProtoBuildProfile not found");

            var protosDirectory = profile.ProtosDirectory;
            var encryptedProtoOutputDirectory = profile.EncryptedProtoOutputDirectory;
            var ignoredDirectories = profile.IgnoredDirectoryNames;

            BuildUtils.ShowProcessBar("构建 Proto", "清理旧输出目录...", 0.1f);
            if (Directory.Exists(encryptedProtoOutputDirectory))
            {
                Directory.Delete(encryptedProtoOutputDirectory, true);
            }
            Directory.CreateDirectory(encryptedProtoOutputDirectory);

            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("构建 Proto", "扫描所有 Proto 文件...", 0.2f);
            var protoFilePaths = Directory.GetFiles(protosDirectory, "*.proto", SearchOption.AllDirectories);

            var processedCount = 0;
            var totalCount = protoFilePaths.Length;

            foreach (var protoPath in protoFilePaths)
            {
                processedCount++;
                var progress = 0.2f + (processedCount / (float)totalCount) * 0.5f;
                var fileName = Path.GetFileName(protoPath);
                BuildUtils.ShowProcessBar("构建 Proto", $"加密 Proto 文件 ({processedCount}/{totalCount}): {fileName}", progress);

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

            BuildUtils.ShowProcessBar("构建 Proto", "添加到 Addressable 组...", 0.85f);
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var assetDirectory = Path.GetRelativePath(projectRoot, encryptedProtoOutputDirectory);
            BuildUtils.AddToAddressableGroup(assetDirectory, profile.AddressableGroupName, profile.AddressableLabel);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("构建 Proto", "完成！", 1f);
        }
    }
}