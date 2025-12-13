using UnityEngine;

namespace XuchFramework.Core
{
    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        [SerializeField]
        protected int _id;

        [SerializeField, Tooltip("默认所属的UI层，留空则使用第一个 UILayer")]
        protected string _defaultLayerName;

        public int ID => _id;
        public string DefaultLayerName => _defaultLayerName;
        public bool IsInitialized { get; private set; }
        public bool IsOpened { get; private set; }
        public bool IsPaused { get; private set; }
        public UILayer CurrentLayer { get; private set; }

        internal void SetLayer(UILayer layer)
        {
            transform.SetParent(layer.transform, false);
            CurrentLayer = layer;
        }

        internal void Init()
        {
            SetVisibilityInternal(false);
            IsPaused = false;
            OnInit();
            IsInitialized = true;
        }

        internal void Dispose()
        {
            OnDispose();
            IsInitialized = false;
        }

        internal void Open()
        {
            SetVisibilityInternal(true);
            OnOpen();
            IsOpened = true;
        }

        internal void Close()
        {
            SetVisibilityInternal(false);
            OnClose();
            IsOpened = false;
        }

        internal void Pause()
        {
            OnPause();
            IsPaused = true;
        }

        public void Resume()
        {
            OnResume();
            IsPaused = false;
        }

        protected virtual void OnInit() { }

        protected virtual void OnOpen() { }

        protected virtual void OnClose() { }

        protected virtual void OnPause() { }

        protected virtual void OnResume() { }

        protected virtual void OnDispose() { }

        private void SetVisibilityInternal(bool isVisible)
        {
            gameObject.SetActive(isVisible);
            IsOpened = isVisible;
        }
    }
}