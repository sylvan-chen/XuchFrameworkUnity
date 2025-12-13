using Cysharp.Threading.Tasks;

namespace XuchFramework.Core.Procedures
{
    public class ProcedureEnterGame : ProcedureBase
    {
        public override void OnProcedureEnter()
        {
            base.OnProcedureEnter();
            // LoadSceneAsync().Forget();
        }

        // private async UniTaskVoid LoadSceneAsync()
        // {
        //     // await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Res/game/scenes/demo002").ToUniTask();
        // }
    }
}