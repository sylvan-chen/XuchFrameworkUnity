using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;

namespace Framework.Core.Editor.AnimationClipCopiers
{
    public class AnimationClipCopier : MonoBehaviour
    {
        [MenuItem("Tools/Copy Animation Clips")]
        public static void CopyAnimationClips()
        {
            Object[] selectedObjects = Selection.objects;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (Object selectedObject in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(selectedObject);

                if (Path.GetExtension(path) == ".fbx" || Path.GetExtension(path) == ".FBX")
                {
                    Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (Object obj in objects)
                    {
                        // 如果对象是 AnimationClip
                        if (obj is AnimationClip)
                        {
                            // 创建新的 AnimationClip
                            AnimationClip newClip = new AnimationClip();

                            // 获取源剪辑
                            AnimationClip srcClip = obj as AnimationClip;

                            // 设置新剪辑的名字
                            newClip.name = selectedObject.name;

                            // 设置新剪辑的帧率
                            newClip.frameRate = srcClip.frameRate;

                            // 复制源剪辑的所有曲线到新剪辑
                            foreach (var binding in AnimationUtility.GetCurveBindings(srcClip))
                            {
                                AnimationUtility.SetEditorCurve(newClip, binding,
                                    AnimationUtility.GetEditorCurve(srcClip, binding));
                            }

                            // 复制源剪辑的所有事件到新剪辑
                            AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(srcClip);
                            AnimationUtility.SetAnimationEvents(newClip, animationEvents);

                            // 创建新剪辑的路径
                            string newPath = Path.GetDirectoryName(path) + "/" + newClip.name + ".anim";

                            // 创建新的 AnimationClip asset
                            // AssetDatabase.CopyAsset(path, newPath);
                            AssetDatabase.CreateAsset(newClip, newPath);

                            // 添加新剪辑到 Addressable Assets
                            settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newPath), settings.DefaultGroup);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }
    }
}