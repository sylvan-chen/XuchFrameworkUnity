using Xuch.Framework.Internal;
using UnityEngine.InputSystem;

namespace Xuch.Framework
{
    public class UIInputAdapter : GameInputActions.IUIActions
    {
        private readonly GameInputActions _gameInput;

        public UIInputAdapter(GameInputActions gameInput)
        {
            _gameInput = gameInput;
            _gameInput.UI.SetCallbacks(this);
        }

        public void Enable() => _gameInput.UI.Enable();
        public void Disable() => _gameInput.UI.Disable();

        public void OnNavigate(InputAction.CallbackContext context) { }
        public void OnSubmit(InputAction.CallbackContext context) { }
        public void OnCancel(InputAction.CallbackContext context) { }
        public void OnPoint(InputAction.CallbackContext context) { }
        public void OnClick(InputAction.CallbackContext context) { }
        public void OnRightClick(InputAction.CallbackContext context) { }
        public void OnMiddleClick(InputAction.CallbackContext context) { }
        public void OnScrollWheel(InputAction.CallbackContext context) { }
        public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
    }
}