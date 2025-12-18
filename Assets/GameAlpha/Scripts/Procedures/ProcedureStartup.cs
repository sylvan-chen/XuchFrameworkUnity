using Cysharp.Threading.Tasks;
using XuchFramework.Core;

namespace Gameplay.Procedures
{
    public class ProcedureStartup : ProcedureBase
    {
        protected override void OnProcedureEnter()
        {
            StartGame().Forget();
        }

        private async UniTaskVoid StartGame()
        {
            await GameRunner.Instance.LaunchManagers("[game_managers]");

            GameProcedure.Instance.ChangeProcedure<ProcedureEnterGame>();
        }
    }
}