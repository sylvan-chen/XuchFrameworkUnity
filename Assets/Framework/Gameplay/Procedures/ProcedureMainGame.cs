using Cysharp.Threading.Tasks;
using DigiEden.Framework;
using DigiEden.Framework.Utils;
using UnityEngine;

public class ProcedureMainGame : ProcedureBase
{
    private int listenerId;
    private GameObject petObj;

    public override void OnEnter(Fsm<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("Enter ProcedureMainGame.");

        TestAsync().Forget();
    }

    private async UniTaskVoid TestAsync()
    {
        listenerId = App.EventManager.AddListener(
            1001,
            (evt) =>
            {
                var msg = evt.Args[0] as string;
                Log.Debug($"Received event 1001 with message: {msg}");
            });

        // App.AudioManager.Play2D("event:/bgm_forsaken");

        // var handle1 = App.AudioManager.Play2DLoop("event:/beep", 5f);
        // App.AudioManager.SetPitch(handle1, 1.5f);

        // var handle2 = App.AudioManager.Play2DLoop("event:/beep", 0.5f);
        // App.AudioManager.SetVolume(handle2, 0.8f);

        await UniTask.WaitForSeconds(5);

        App.EventManager.Dispatch(1001, "Hello from ProcedureMainGame 1!");

        await UniTask.WaitForSeconds(5);

        App.EventManager.Dispatch(1001, "Hello from ProcedureMainGame 2!");
    }

    public override void OnExit(Fsm<ProcedureManager> fsm)
    {
        base.OnExit(fsm);

        App.EventManager.RemoveListener(listenerId);
        if (petObj != null)
            App.ResourceManager.DestroyInstance(petObj);

        App.AudioManager.StopAll();
    }
}