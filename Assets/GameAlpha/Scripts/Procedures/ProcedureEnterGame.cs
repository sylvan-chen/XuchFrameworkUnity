using Cysharp.Threading.Tasks;
using XuchFramework.Core;

namespace Gameplay.Procedures
{
    public class ProcedureEnterGame : ProcedureBase
    {
        protected override void OnProcedureEnter()
        {
            // LoadSceneAsync().Forget();
        }

        // private async UniTaskVoid LoadSceneAsync()
        // {
        //     // await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Res/game/scenes/demo002").ToUniTask();
        // }
    }
}