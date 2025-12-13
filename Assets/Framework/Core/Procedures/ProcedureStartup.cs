using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core.Procedures
{
    public class ProcedureStartup : ProcedureBase
    {
        public override void OnProcedureEnter()
        {
            base.OnProcedureEnter();
            StartGame().Forget();
        }

        private async UniTaskVoid StartGame()
        {
            var gameManagerRoot = GameObject.Find("[game_managers]");
            if (gameManagerRoot == null)
            {
                Log.Error("[ProcedureStartup] Start game failed. Can't find root for game managers. (Expected root name: '[game_managers]')");
                return;
            }

            var gameManagers = gameManagerRoot.GetComponentsInChildren<ManagerBase>();
            Log.Debug($"[ProcedureStartup] Found {gameManagers.Length} game managers.");

            foreach (var manager in gameManagers)
            {
                GameRunner.Instance.RegisterManager(manager);
                await manager.Initialize();
            }

            foreach (var manager in gameManagers)
            {
                await manager.PostInitialize();
            }

            GameModule<ProcedureManager>.Instance.ChangeProcedure<ProcedureEnterGame>();
        }
    }
}