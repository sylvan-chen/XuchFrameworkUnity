using UnityEditor;
using UnityEngine;
using System.Linq;

namespace XuchFramework.Editor
{
    public class CopyAllComponent : EditorWindow
    {
        private enum PasteMode
        {
            PasteAll,
            PasteAsNewOnly,
            PasteValuesOnly,
        }

        private const string SESSION_KEY_COMPONENT_IDS = "CopyAllComponent_ComponentIds";
        private const string SESSION_KEY_COUNT = "CopyAllComponent_Count";

        private static Component[] copiedComponents;

        [MenuItem("GameObject/Copy All Components #&C", priority = 1000)]
        private static void Copy()
        {
            copiedComponents = Selection.activeGameObject.GetComponents<Component>();
            SaveComponentsToSession(copiedComponents);

            Debug.Log($"已复制以下组件（{copiedComponents.Length} 个）：");
            foreach (var component in copiedComponents)
            {
                if (!component)
                    continue;
                Debug.Log(component.GetType().Name);
            }
        }

        [MenuItem("GameObject/Paste All Components #&V", priority = 1001)]
        private static void Paste()
        {
            PasteInternal(PasteMode.PasteAll);
        }

        [MenuItem("GameObject/Paste All Components (As New Only)", priority = 1002)]
        private static void PasteAsNewOnly()
        {
            PasteInternal(PasteMode.PasteAsNewOnly);
        }

        [MenuItem("GameObject/Paste All Components (Values Only)", priority = 1003)]
        private static void PasteValuesOnly()
        {
            PasteInternal(PasteMode.PasteValuesOnly);
        }

        private static void PasteInternal(PasteMode mode)
        {
            // 如果内存中没有，尝试从 SessionState 加载
            copiedComponents ??= LoadComponentsFromSession();

            if (copiedComponents == null || copiedComponents.Length == 0)
            {
                Debug.LogWarning("没有复制的组件可粘贴。请先复制组件");
                return;
            }

            foreach (var targetGameObject in Selection.gameObjects)
            {
                if (targetGameObject == null)
                    continue;
                Debug.Log($"开始粘贴 {copiedComponents.Length} 个 组件到以下对象：{targetGameObject.name}");

                foreach (var copiedComponent in copiedComponents)
                {
                    if (!copiedComponent)
                        continue;
                    UnityEditorInternal.ComponentUtility.CopyComponent(copiedComponent);

                    // Check if the targetGameObject already has the component
                    var existingComponent = targetGameObject.GetComponent(copiedComponent.GetType());
                    if (existingComponent == null)
                    {
                        if (mode == PasteMode.PasteValuesOnly)
                            continue;
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetGameObject);
                        Debug.Log("已粘贴新组件: " + copiedComponent.GetType().Name);
                    }
                    else
                    {
                        if (mode == PasteMode.PasteAsNewOnly)
                            continue;
                        UnityEditorInternal.ComponentUtility.PasteComponentValues(existingComponent);
                        Debug.Log("已更新现有组件: " + copiedComponent.GetType().Name);
                    }
                }
            }
        }

        private static void SaveComponentsToSession(Component[] components)
        {
            if (components == null || components.Length == 0)
            {
                SessionState.EraseIntArray(SESSION_KEY_COMPONENT_IDS);
                SessionState.EraseInt(SESSION_KEY_COUNT);
                return;
            }

            int[] instanceIds = components.Where(c => c != null).Select(c => c.GetInstanceID()).ToArray();
            SessionState.SetIntArray(SESSION_KEY_COMPONENT_IDS, instanceIds);
            SessionState.SetInt(SESSION_KEY_COUNT, instanceIds.Length);
        }

        private static Component[] LoadComponentsFromSession()
        {
            int count = SessionState.GetInt(SESSION_KEY_COUNT, 0);
            if (count == 0)
                return null;

            int[] instanceIds = SessionState.GetIntArray(SESSION_KEY_COMPONENT_IDS, new int[0]);
            if (instanceIds.Length == 0)
                return null;

            Component[] components = instanceIds.Select(id => EditorUtility.InstanceIDToObject(id) as Component).Where(c => c != null).ToArray();

            return components.Length > 0 ? components : null;
        }
    }
}