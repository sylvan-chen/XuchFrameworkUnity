using Cysharp.Threading.Tasks;
using XuchFramework.Core;

namespace GamePlay
{
    public class DefaultGameEntry : GameEntryBase
    {
        public override async UniTask EnterGame()
        {
            await GameRunner.Instance.LaunchModules("[game_modules]");
        }
    }
}