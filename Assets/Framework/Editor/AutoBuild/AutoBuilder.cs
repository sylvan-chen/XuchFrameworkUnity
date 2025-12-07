using System;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    public static class AutoBuilder
    {
        private static BuildConfig _currentConfig;

        /// <summary>
        /// 开始构建
        /// </summary>
        public static void StartBuild(BuildConfig buildConfig)
        {
            try
            {
                _currentConfig = buildConfig;
                if (_currentConfig == null)
                {
                    throw new ArgumentNullException(nameof(buildConfig), "[AutoBuilder] 构建配置不能为空");
                }

                BuildPipeline_ApplyBuildConfig.Run(_currentConfig);
                BuildPipeline_ApplyPlatformSettings.Run(_currentConfig);
                BuildPipeline_BuildLua.Run(_currentConfig);
                BuildPipeline_BuildProto.Run(_currentConfig);
                BuildPipeline_BuildAddressables.Run(_currentConfig);
                BuildPipeline_BuildPlayer.Run(_currentConfig);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoBuilder] 构建失败: {e.Message}\n{e.StackTrace}");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}