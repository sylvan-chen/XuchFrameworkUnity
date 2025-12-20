namespace XuchFramework.Core
{
    public class GameModule<T> where T : ModuleBase
    {
        public static T Instance { get; private set; }

        internal static void SetInstance(T instance)
        {
            Instance = instance;
        }
    }
}