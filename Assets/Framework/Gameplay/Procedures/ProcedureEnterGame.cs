using Cysharp.Threading.Tasks;
using DigiEden.Framework;
using DigiEden.Framework.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProcedureEnterGame : ProcedureBase
{
    public override void OnEnter(Fsm<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);
        Log.Debug("Enter ProcedureEnterGame.");
        // LoadSceneAsync(fsm).Forget();
    }

    // private async UniTaskVoid LoadSceneAsync(Fsm<ProcedureManager> fsm)
    // {
    //     // 加载场景
    //     await SceneManager.LoadSceneAsync("Res/game/scenes/game001").ToUniTask();

    //     ChangeState<ProcedureMainGame>(fsm);
    // }
}