using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UI;
using XuchFramework.Core.Utils;

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

        [SerializeField, Tooltip("UI层名称，留空使用 GameObject 名称")]
        private string _layerName;

        [Button]
        private void UseGameObjectName() => _layerName = name;

        [SerializeField, Tooltip("当有新面板覆盖在栈顶时，栈顶面板的行为")]
        private TopCoverBehaviour _onTopPanelCovered;

        public string LayerName => _layerName;
        public Canvas Canvas { get; private set; }

        private readonly Stack<UIPanelBase> _openedPanelStack = new(); // 管理打开的面板

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

        /// <summary>
        /// 将面板推入当前层的栈顶
        /// </summary>
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

        /// <summary>
        /// 移除指定的面板
        /// </summary>
        public void PopPanel(UIPanelBase panel)
        {
            if (panel == null)
            {
                Log.Error($"[UILayer] Remove panel from layer '{LayerName}' failed. Panel is null.");
                return;
            }

            if (_openedPanelStack.Count == 0 || !_openedPanelStack.Contains(panel))
                return;

            if (_openedPanelStack.Peek() == panel) // 如果要移除的面板是栈顶面板，则需要恢复上一个面板
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
            else // 如果要移除的面板不是栈顶面板，则直接从栈中移除
            {
                var tempStack = new Stack<UIPanelBase>();

                // 将栈顶元素弹出，直到找到要移除的面板
                while (_openedPanelStack.Count > 0)
                {
                    var currentPanel = _openedPanelStack.Pop();
                    if (currentPanel != panel)
                    {
                        tempStack.Push(currentPanel);
                    }
                }

                // 恢复之前弹出的面板
                while (tempStack.Count > 0)
                {
                    var remainingPanel = tempStack.Pop();
                    _openedPanelStack.Push(remainingPanel);
                }
            }
        }
    }
}