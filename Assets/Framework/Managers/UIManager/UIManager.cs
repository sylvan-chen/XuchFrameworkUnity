using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using Xuch.Extensions;
using UnityEngine.Rendering;
using System.Linq;
using Cysharp.Threading.Tasks;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/UI Manager")]
    public sealed class UIManager : ManagerBase
    {
        public enum UICameraType
        {
            MainCamera = 0,
            CreateOnInit,
            Overlay,
        }

        [SerializeField]
        private Transform _uiRoot;

        [SerializeField]
        private UICameraType _uiCameraType = UICameraType.MainCamera;

        private Camera _uiCamera;
        private readonly List<UILayer> _uiLayers = new();
        private readonly Dictionary<int, UIPanelBase> _allPanels = new();
        private readonly Dictionary<int, UIPanelBase> _openedPanels = new();

        protected override void OnInitialize()
        {
            InitUIRoot();
            InitUICamera();
            InitUILayers();
        }

        protected override void OnDispose()
        {
            foreach (var layer in _uiLayers)
            {
                layer.Dispose();
            }

            foreach (var panel in _allPanels.Values)
            {
                App.ResourceManager.DestroyInstance(panel.gameObject);
            }

            _allPanels.Clear();
            _openedPanels.Clear();
        }

        private void InitUIRoot()
        {
            if (_uiRoot == null)
            {
                Log.Error("[UIManager] UI root is null.");
            }

            if (_uiRoot.parent == null)
                Object.DontDestroyOnLoad(_uiRoot.gameObject);
        }

        private void InitUICamera()
        {
            switch (_uiCameraType)
            {
                case UICameraType.MainCamera:
                    _uiCamera = Camera.main;
                    break;
                case UICameraType.CreateOnInit:
                    _uiCamera = CreateNewUICamera();
                    break;
                case UICameraType.Overlay:
                default:
                    _uiCamera = null;
                    break;
            }
        }

        private Camera CreateNewUICamera()
        {
            // 排除主相机 UI 层级的渲染
            Camera.main.ExcludeLayer("UI");
            // 创建 UI 层级专用的摄像机
            var cameraObj = new GameObject("[UICamera]") { layer = LayerMask.NameToLayer("UI") };
            cameraObj.transform.SetParent(_uiRoot);
            cameraObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var uiCamera = cameraObj.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.Depth;            // 使用深度清除
            uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // 只渲染UI层
            uiCamera.orthographic = true;                            // 使用正交投影
            uiCamera.depth = 100;                                    // 确保在其他摄像机之上
            uiCamera.useOcclusionCulling = false;                    // 不需要遮挡剔除，节约性能

            // URP: 把 UICamera 添加到主相机的渲染堆栈中
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                var mainCamData = Camera.main.GetUniversalAdditionalCameraData();
                var uiCamData = uiCamera.GetUniversalAdditionalCameraData();

                mainCamData.renderType = CameraRenderType.Base;
                uiCamData.renderType = CameraRenderType.Overlay;

                if (!mainCamData.cameraStack.Contains(uiCamera))
                    mainCamData.cameraStack.Add(uiCamera);

                uiCamData.renderShadows = false;
            }

            return uiCamera;
        }

        private void InitUILayers()
        {
            var layers = _uiRoot.GetComponentsInChildren<UILayer>();

            foreach (var layer in layers)
            {
                layer.Init(_uiCamera);
                _uiLayers.Add(layer);
            }
        }

        /// <summary>
        /// 获取 UI Layer
        /// </summary>
        public UILayer GetUILayer(string layerName)
        {
            var layer = _uiLayers.FirstOrDefault(x => x.name == layerName);
            if (layer == null)
                Log.Error($"[UIManager] UILayer '{layerName}' not found.");
            return layer;
        }

        /// <summary>
        /// 重设 UI 相机
        /// </summary>
        public void ResetUICamera(Camera targetCamera = null)
        {
            targetCamera ??= Camera.main;
            _uiCamera = targetCamera;
            foreach (var layer in _uiLayers)
            {
                layer.SetWorldCamera(_uiCamera);
            }
        }

        /// <summary>
        /// 异步加载面板
        /// </summary>
        public async UniTask<UIPanelBase> LoadPanelAsync(string path)
        {
            var panelObj = await App.ResourceManager.InstantiateAsync(path);
            if (panelObj == null)
            {
                Log.Error($"[UIManager] Failed to load panel from path: {path}");
                return null;
            }

            if (!panelObj.TryGetComponent<UIPanelBase>(out var panel))
            {
                Log.Error($"[UIManager] Panel prefab missing 'PanelBase' component: {path}.");
                App.ResourceManager.DestroyInstance(panelObj);
                return null;
            }

            panel.Init();

            var layer = GetUILayer(panel.DefaultLayerName) ?? _uiLayers.FirstOrDefault();
            if (layer == null)
            {
                Log.Error($"[UIManager] No UILayer found for panel: {panel.ID}.");
                App.ResourceManager.DestroyInstance(panelObj);
                return null;
            }
            panel.SetLayer(layer);

            _allPanels[panel.ID] = panel;
            return panel;
        }

        /// <summary>
        /// 卸载面板
        /// </summary>
        public void UnloadPanel(int id)
        {
            if (_allPanels.TryGetValue(id, out var loadedPanel))
            {
                ClosePanel(id);
                loadedPanel.Dispose();
                _allPanels.Remove(id);
                App.ResourceManager.DestroyInstance(loadedPanel.gameObject);
            }
        }

        /// <summary>
        /// 卸载面板
        /// </summary>
        public void UnloadPanel(UIPanelBase panel)
        {
            if (panel == null)
                return;
            UnloadPanel(panel.ID);
        }

        /// <summary>
        /// 获取 UI 面板
        /// </summary>
        public UIPanelBase GetPanel(int id)
        {
            return _allPanels.GetValueOrDefault(id);
        }

        /// <summary>
        /// 打开 UI 面板
        /// </summary>
        /// <param name="id">面板 ID</param>
        public UIPanelBase OpenPanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel)) // 已经打开
            {
                return openedPanel;
            }
            else if (_allPanels.TryGetValue(id, out var loadedPanel)) // 已经加载但未打开
            {
                var layer = loadedPanel.CurrentLayer;
                if (layer == null)
                {
                    Log.Error($"[UIManager] UILayer for panel({id}) not found.");
                    return null;
                }

                layer.PushPanel(loadedPanel);
                loadedPanel.Open();
                _openedPanels[id] = loadedPanel;
                return loadedPanel;
            }
            else
            {
                Log.Error($"[UIManager] Panel({id}) not found.");
                return null;
            }
        }

        /// <summary>
        /// 关闭 UI 面板
        /// </summary>
        public void ClosePanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                openedPanel.CurrentLayer.PopPanel(openedPanel);
                openedPanel.Close();
                _openedPanels.Remove(id);
            }
        }

        /// <summary>
        /// 关闭 UI 面板
        /// </summary>
        public void ClosePanel(UIPanelBase panel)
        {
            if (panel == null)
                return;
            ClosePanel(panel.ID);
        }

        /// <summary>
        /// 切换 UI 面板所属的层级
        /// </summary>
        public void ChangePanelLayer(UIPanelBase panel, string targetLayerName)
        {
            if (panel == null)
                return;

            var newLayer = GetUILayer(targetLayerName);
            if (newLayer == null)
            {
                Log.Error($"[UIManager] UILayer '{targetLayerName}' not found.");
                return;
            }

            var oldLayer = panel.CurrentLayer;
            if (oldLayer != null)
                oldLayer.PopPanel(panel);
            panel.SetLayer(newLayer);

            // 只有在面板已经打开的情况下，才将其推入新层级的栈顶
            if (panel.IsOpened)
            {
                newLayer.PushPanel(panel);
            }
        }

        /// <summary>
        /// 恢复 UI 面板所属层级到其默认层级
        /// </summary>
        public void RestorePanelLayer(UIPanelBase panel)
        {
            if (panel == null)
                return;
            ChangePanelLayer(panel, panel.DefaultLayerName);
        }
    }
}