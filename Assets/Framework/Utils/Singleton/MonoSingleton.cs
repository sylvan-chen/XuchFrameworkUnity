using UnityEngine;

namespace DigiEden.Framework.Utils
{
    public interface IInitializable
    {
        bool IsInitialized { get; }
        void Startup();
    }

    public enum InitState
    {
        None,
        Initializing,
        Inited
    }

    [DisallowMultipleComponent]
    public abstract class MonoInitSingleton<T> : MonoBehaviour where T : MonoInitSingleton<T>
    {
        private static T mInstance = null;
        [SerializeField]
        private InitState mInitState = InitState.None;

        public InitState initState
        {
            get { return mInitState; }
            set { mInitState = value; }
        }

        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = GameObject.FindFirstObjectByType(typeof(T)) as T;
                    if (mInstance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        var component = go.AddComponent<T>();
                        GameObject parent = GameObject.Find("Boot");
                        if (parent == null)
                        {
                            parent = new GameObject("Boot");
                        }
                        if (parent != null)
                        {
                            go.transform.parent = parent.transform;
                        }
                        return component;
                    }
                }

                return mInstance;
            }
        }

        private void Awake()
        {
            if (mInstance == null)
            {
                mInstance = this as T;
            }
            else if (mInstance != this)
            {
                Debug.LogWarning($"Instance of {typeof(T).Name} already exists, destroying duplicate!");
                Destroy(gameObject);
                return;
            }

            // DontDestroyOnLoad(gameObject);
            Init();
        }

        protected virtual void Init()
        {
            initState = InitState.Initializing;
        }

        public void DestroySelf()
        {
            Dispose();
            MonoInitSingleton<T>.mInstance = null;
            UnityEngine.Object.Destroy(gameObject);
        }

        public virtual void Dispose()
        {
            Debug.Log($"{typeof(T).Name} Dispose");
        }

        private void OnDisable()
        {
            Dispose();
        }
    }

    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static bool _notFound = false;

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
        }
    }

    /// <summary>
    /// 持久化单例，在场景切换时，实例不会被销毁
    /// </summary>
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