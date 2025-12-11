using UnityEngine.InputSystem;
using Xuch.Framework.Internal;

namespace Xuch.Framework
{
    /// <summary>
    /// XR 输入适配器
    /// </summary>
    public class HandInputAdapter : GameInputActions.IHandActions
    {
        private readonly GameInputActions _gameInput;

        public HandInputAdapter(GameInputActions gameInput)
        {
            _gameInput = gameInput;
            _gameInput.Hand.SetCallbacks(this);
        }

        public void Enable() => _gameInput.Hand.Enable();
        public void Disable() => _gameInput.Hand.Disable();

        public void OnGripPressedR(InputAction.CallbackContext context) { }
        public void OnGripReleasedR(InputAction.CallbackContext context) { }
        public void OnTriggerPressedR(InputAction.CallbackContext context) { }
        public void OnTriggerReleasedR(InputAction.CallbackContext context) { }
        public void OnPrimaryPressedR(InputAction.CallbackContext context) { }
        public void OnPrimaryReleasedR(InputAction.CallbackContext context) { }
        public void OnSecondaryPressedR(InputAction.CallbackContext context) { }
        public void OnSecondaryReleasedR(InputAction.CallbackContext context) { }
        public void OnGripPressedL(InputAction.CallbackContext context) { }
        public void OnGripReleasedL(InputAction.CallbackContext context) { }
        public void OnTriggerPressedL(InputAction.CallbackContext context) { }
        public void OnTriggerReleasedL(InputAction.CallbackContext context) { }
        public void OnPrimaryPressedL(InputAction.CallbackContext context) { }
        public void OnPrimaryReleasedL(InputAction.CallbackContext context) { }
        public void OnSecondaryPressedL(InputAction.CallbackContext context) { }
        public void OnSecondaryReleasedL(InputAction.CallbackContext context) { }
        public void OnGripAxisR(InputAction.CallbackContext context) { }
        public void OnGripAxisL(InputAction.CallbackContext context) { }
        public void OnTriggerAxisR(InputAction.CallbackContext context) { }
        public void OnTriggerAxisL(InputAction.CallbackContext context) { }
        public void OnHapticR(InputAction.CallbackContext context) { }
        public void OnHapticL(InputAction.CallbackContext context) { }
    }
}