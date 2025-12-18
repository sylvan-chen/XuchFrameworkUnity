using System;
using UnityEditor;
using XuchFramework.Core;

namespace XuchFramework.Editor
{
    public static class AutoBuilder
    {
        private static BuildConfig _currentConfig;

        public static void StartBuild(BuildConfig buildConfig)
        {
            try
            {
                _currentConfig = buildConfig;
                if (_currentConfig == null)
                {
                    throw new ArgumentNullException(nameof(buildConfig), "[AutoBuilder] BuildConfig cannot be null");
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
                Log.Error($"[AutoBuilder] Build Failed: {e.Message}\n{e.StackTrace}");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}