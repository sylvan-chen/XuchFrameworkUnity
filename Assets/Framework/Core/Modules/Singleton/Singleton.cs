namespace XuchFramework.Core.Utils
{
    /// <summary>
    /// Singleton base class for regular C# classes
    /// </summary>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static bool _notFound = false;

        private static readonly object _lock = new();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null && !_notFound)
                    {
                        _instance = new T();
                        (_instance as Singleton<T>)?.OnInit();
                    }

                    if (_instance == null)
                        _notFound = true;

                    return _instance;
                }
            }
        }

        public static void DestroyInstance()
        {
            if (_instance == null)
                return;

            lock (_lock)
            {
                if (_instance == null)
                    return;

                (_instance as Singleton<T>)?.OnDestroy();
                _instance = null;
            }
        }

        protected virtual void OnInit() { }

        protected virtual void OnDestroy() { }

        /// <summary>
        /// Prevent external instantiation
        /// </summary>
        protected Singleton() { }
    }
}