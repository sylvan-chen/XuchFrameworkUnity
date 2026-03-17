using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Framework.Core
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
        [SerializeField]
        private bool _isPersistent = false;

        [Space(5)]
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
                    if (_instance == null)
                    {
                        _instance = new GameObject().AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            InitializeAsync().Forget();

            async UniTaskVoid InitializeAsync()
            {
                MakeSingleton();
                if (_isPersistent && gameObject != null && transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                _singletonState = MonoSingletonState.Initializing;
                OnInitialize();
                await OnInitializeAsync();
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

        protected virtual void OnInitialize() { }

        protected virtual UniTask OnInitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnDispose() { }
    }
}