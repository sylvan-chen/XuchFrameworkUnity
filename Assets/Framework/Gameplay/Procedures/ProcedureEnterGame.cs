using Cysharp.Threading.Tasks;
using Xuch.Framework;
using Xuch.Framework.Utils;

public class ProcedureEnterGame : ProcedureBase
{
    public override void OnEnter(Fsm<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);
        Log.Debug("Enter ProcedureEnterGame.");
        LoadSceneAsync(fsm).Forget();
    }

    private async UniTaskVoid LoadSceneAsync(Fsm<ProcedureManager> fsm)
    {
        // 加载场景
        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Res/game/scenes/demo002").ToUniTask();
    }
}