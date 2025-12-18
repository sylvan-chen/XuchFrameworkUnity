using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core
{
    public abstract class GameEntryBase : MonoBehaviour
    {
        public abstract UniTask EnterGame();
    }
}