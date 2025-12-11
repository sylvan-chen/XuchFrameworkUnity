using Xuch.Framework.Internal;
using UnityEngine.InputSystem;

namespace Xuch.Framework
{
    public class PlayerInputAdapter : GameInputActions.IPlayerActions
    {
        private readonly GameInputActions _gameInput;

        public PlayerInputAdapter(GameInputActions gameInput)
        {
            _gameInput = gameInput;
            _gameInput.Player.SetCallbacks(this);
        }

        public void Enable() => _gameInput.Player.Enable();
        public void Disable() => _gameInput.Player.Disable();

        public void OnMove(InputAction.CallbackContext context) { }
        public void OnLook(InputAction.CallbackContext context) { }
        public void OnAttack(InputAction.CallbackContext context) { }
        public void OnInteract(InputAction.CallbackContext context) { }
        public void OnCrouch(InputAction.CallbackContext context) { }
        public void OnJump(InputAction.CallbackContext context) { }
        public void OnJumpRelease(InputAction.CallbackContext context) { }
        public void OnPrevious(InputAction.CallbackContext context) { }
        public void OnNext(InputAction.CallbackContext context) { }
        public void OnSprint(InputAction.CallbackContext context) { }
    }
}