using UnityEngine;

namespace Framework.Extensions.InputSystem
{
    public class GameInputRunner : MonoBehaviour
    {
        private void Start()
        {
            GameInput.Initialize();
        }

        private void OnDestroy()
        {
            GameInput.Dispose();
        }

        private void Update()
        {
            GameInput.Update();
        }
    }
}