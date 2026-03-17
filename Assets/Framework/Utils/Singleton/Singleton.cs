namespace Framework.Utils
{
    /// <summary>
    /// Singleton base class for regular C# classes
    /// </summary>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;

        private static readonly object _lock = new();

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                        var target = _instance as Singleton<T>;
                        target?.OnInitialize();
                    }

                    return _instance;
                }
            }
        }

        public static void DestroyInstance()
        {
            if (_instance == null) return;

            lock (_lock)
            {
                if (_instance == null) return;

                (_instance as Singleton<T>)?.OnDestroy();
                _instance = null;
            }
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnDestroy() { }

        /// <summary>
        /// Prevent external instantiation
        /// </summary>
        protected Singleton() { }
    }
}