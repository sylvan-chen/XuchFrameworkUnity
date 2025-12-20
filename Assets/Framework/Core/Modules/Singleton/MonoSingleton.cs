using Alchemy.Inspector;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core
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

        protected static T _instance;

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_6000_0_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            InitializeAsync().Forget();
            
            async UniTaskVoid InitializeAsync()
            {
                MakeSingleton();
                _singletonState = MonoSingletonState.Initializing;
                await OnInitialize();
                _singletonState = MonoSingletonState.Initialized;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            OnDispose();
        }

        protected void MakeSingleton()
        {
            // Make sure the instance is unique
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        protected virtual UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnDispose() { }
    }

    /// <summary>
    /// Persistent singleton, will not be destroyed on scene load
    /// </summary>
    [DisallowMultipleComponent]
    public class MonoSingletonPersistent<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            InitializeAsync().Forget();
            
            async UniTaskVoid InitializeAsync()
            {
                MakeSingleton();
                if (gameObject != null && transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                _singletonState = MonoSingletonState.Initializing;
                await OnInitialize();
                _singletonState = MonoSingletonState.Initialized;
            }
        }
    }
}