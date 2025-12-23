using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace XuchFramework.Core
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UILayer : MonoBehaviour
    {
        public enum TopCoverBehaviour
        {
            None = 0,
            Close = 1,
            Pause = 2,
            PauseAndClose = 3,
        }

        [SerializeField, Tooltip("UI layer name, use GameObject name if empty")]
        private string _layerName;

        [Button]
        private void UseGameObjectName() => _layerName = name;

        [SerializeField, Tooltip("Behaviour of top panel when covered by a new panel")]
        private TopCoverBehaviour _onTopPanelCovered;

        public string LayerName => _layerName;
        public Canvas Canvas { get; private set; }

        private readonly Stack<UIPanelBase> _openedPanelStack = new();

        public void Init(Camera uiCamera)
        {
            Canvas = GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.worldCamera = uiCamera;
            _openedPanelStack.Clear();
        }

        public void Dispose()
        {
            while (_openedPanelStack.Count > 0)
            {
                var panel = _openedPanelStack.Pop();
                panel.Close();
            }
        }

        public void SetWorldCamera(Camera cam)
        {
            if (Canvas != null)
                Canvas.worldCamera = cam;
        }

        public void PushPanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[UILayer] Push panel to layer '{LayerName}' failed. Panel is null.");
                return;
            }

            if (_openedPanelStack.Count > 0)
            {
                var topPanel = _openedPanelStack.Peek();
                if (_onTopPanelCovered is TopCoverBehaviour.Close or TopCoverBehaviour.PauseAndClose)
                {
                    topPanel.Close();
                }

                if (_onTopPanelCovered is TopCoverBehaviour.Pause or TopCoverBehaviour.PauseAndClose)
                {
                    topPanel.Pause();
                }
            }

            _openedPanelStack.Push(panel);
        }

        public void PopPanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[UILayer] Remove panel from layer '{LayerName}' failed. Panel is null.");
                return;
            }

            if (_openedPanelStack.Count == 0 || !_openedPanelStack.Contains(panel))
                return;

            // If removing the top panel, restore the previous panel
            if (_openedPanelStack.Peek() == panel)
            {
                _openedPanelStack.Pop();
                if (_openedPanelStack.Count > 0)
                {
                    var topPanel = _openedPanelStack.Peek();
                    if (_onTopPanelCovered is TopCoverBehaviour.Close or TopCoverBehaviour.PauseAndClose)
                    {
                        topPanel.Open();
                    }

                    if (_onTopPanelCovered is TopCoverBehaviour.Pause or TopCoverBehaviour.PauseAndClose)
                    {
                        topPanel.Resume();
                    }
                }
            }
            else
            {
                var tempStack = new Stack<UIPanelBase>();

                while (_openedPanelStack.Count > 0)
                {
                    var currentPanel = _openedPanelStack.Pop();
                    if (currentPanel != panel)
                    {
                        tempStack.Push(currentPanel);
                    }
                }

                while (tempStack.Count > 0)
                {
                    var remainingPanel = tempStack.Pop();
                    _openedPanelStack.Push(remainingPanel);
                }
            }
        }
    }
}