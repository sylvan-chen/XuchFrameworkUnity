using Cysharp.Threading.Tasks;
using XuchFramework.Core;

namespace Gameplay.Procedures
{
    public class ProcedureEnterGame : ProcedureBase
    {
        protected override void OnProcedureEnter()
        {
            LoadSceneAsync().Forget();
        }

        private async UniTaskVoid LoadSceneAsync()
        {
            // await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Res/game/scenes/demo002").ToUniTask();

            var caches = CachePool.Instance.Acquire<TestCache>(10);

            await UniTask.Delay(10000);

            CachePool.Instance.Release(caches.GetRange(0, 5));
        }

        public class TestCache : ICache
        {
            public int Id = 0;
        }
    }
}