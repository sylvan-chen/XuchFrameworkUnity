using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Core
{
    public abstract class GameEntryBase : MonoBehaviour
    {
        public abstract UniTask EnterGame();
    }
}