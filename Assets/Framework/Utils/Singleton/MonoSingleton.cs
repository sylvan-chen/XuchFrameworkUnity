using Alchemy.Inspector;
using UnityEngine;

namespace Xuch.Framework.Utils
{
    public enum MonoSingletonState
    {
        None,
        Initializing,
        Initialized
    }

    [DisallowMultipleComponent]
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        protected MonoSingletonState _singletonState;

        public MonoSingletonState SingletonState => _singletonState;

        protected static bool _notFound = false;
        protected static T _instance;

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_instance == null && !_notFound)
                {
#if UNITY_6000_0_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                }

                if (_instance == null)
                    _notFound = true;

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // 确保实例唯一
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            _singletonState = MonoSingletonState.Initializing;
        }
    }

    /// <summary>
    /// 持久化单例，在场景切换时，实例不会被销毁
    /// </summary>
    [DisallowMultipleComponent]
    public class MonoSingletonPersistent<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            if (gameObject != null && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}