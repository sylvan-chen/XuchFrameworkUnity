using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using XuchFramework.Extensions;
using UnityEngine.Rendering;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Managers/UI Manager")]
    public sealed class UIManager : ModuleBase
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
                GameModule<ResourceManager>.Instance.DestroyInstance(panel.gameObject);
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
            // Exclude UI layer from main camera
            Camera.main.ExcludeLayer("UI");
            // Create new UI camera for UI rendering
            var cameraObj = new GameObject("[UICamera]") { layer = LayerMask.NameToLayer("UI") };
            cameraObj.transform.SetParent(_uiRoot);
            cameraObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var uiCamera = cameraObj.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.Depth;            // Use depth clear
            uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // Only render UI layer
            uiCamera.orthographic = true;                            // Use orthographic projection
            uiCamera.depth = 100;                                    // Ensure on top of other cameras
            uiCamera.useOcclusionCulling = false;                    // No occlusion culling to save performance

            // For URP: Add UICamera to main camera's render stack
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

        public UILayer GetUILayer(string layerName)
        {
            var layer = _uiLayers.FirstOrDefault(x => x.name == layerName);
            if (layer == null)
                Log.Error($"[UIManager] UILayer '{layerName}' not found.");
            return layer;
        }

        public void ResetUICamera(Camera targetCamera = null)
        {
            targetCamera ??= Camera.main;
            _uiCamera = targetCamera;
            foreach (var layer in _uiLayers)
            {
                layer.SetWorldCamera(_uiCamera);
            }
        }

        public async UniTask<UIPanelBase> LoadPanelAsync(string path)
        {
            var panelObj = await GameModule<ResourceManager>.Instance.InstantiateAsync(path);
            if (panelObj == null)
            {
                Log.Error($"[UIManager] Failed to load panel from path: {path}");
                return null;
            }

            if (!panelObj.TryGetComponent<UIPanelBase>(out var panel))
            {
                Log.Error($"[UIManager] Panel prefab missing 'PanelBase' component: {path}.");
                GameModule<ResourceManager>.Instance.DestroyInstance(panelObj);
                return null;
            }

            panel.Init();

            var layer = GetUILayer(panel.DefaultLayerName) ?? _uiLayers.FirstOrDefault();
            if (layer == null)
            {
                Log.Error($"[UIManager] No UILayer found for panel: {panel.ID}.");
                GameModule<ResourceManager>.Instance.DestroyInstance(panelObj);
                return null;
            }
            panel.SetLayer(layer);

            _allPanels[panel.ID] = panel;
            return panel;
        }

        public void UnloadPanel(int id)
        {
            if (_allPanels.TryGetValue(id, out var loadedPanel))
            {
                ClosePanel(id);
                loadedPanel.Dispose();
                _allPanels.Remove(id);
                GameModule<ResourceManager>.Instance.DestroyInstance(loadedPanel.gameObject);
            }
        }

        public void UnloadPanel(UIPanelBase panel)
        {
            if (panel == null)
                return;
            UnloadPanel(panel.ID);
        }

        public UIPanelBase GetPanel(int id)
        {
            return _allPanels.GetValueOrDefault(id);
        }

        public UIPanelBase OpenPanel(int id)
        {
            // Already opened
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                return openedPanel;
            }
            // Already loaded but not opened
            else if (_allPanels.TryGetValue(id, out var loadedPanel))
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

        public void ClosePanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                openedPanel.CurrentLayer.PopPanel(openedPanel);
                openedPanel.Close();
                _openedPanels.Remove(id);
            }
        }

        public void ClosePanel(UIPanelBase panel)
        {
            if (panel == null)
                return;
            ClosePanel(panel.ID);
        }

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

            // Only push to the new layer stack if the panel is already opened
            if (panel.IsOpened)
            {
                newLayer.PushPanel(panel);
            }
        }

        public void RestorePanelLayer(UIPanelBase panel)
        {
            if (panel == null)
                return;
            ChangePanelLayer(panel, panel.DefaultLayerName);
        }
    }
}