using UnityEngine;
using UnityEngine.InputSystem;

namespace DigiEden.Gameplay
{
    public class HandPlayerControllerLink : MonoBehaviour
    {
        public HandPlayer player;

        [Header("Input")]
        public InputActionProperty moveAxis;
        public InputActionProperty turnAxis;

        private void OnEnable()
        {
            if (moveAxis.action != null)
                moveAxis.action.Enable();
            if (moveAxis.action != null)
                moveAxis.action.performed += MoveAction;
            if (turnAxis.action != null)
                turnAxis.action.Enable();
            if (turnAxis.action != null)
                turnAxis.action.performed += TurnAction;
        }

        private void OnDisable()
        {
            if (moveAxis.action != null)
                moveAxis.action.performed -= MoveAction;
            if (turnAxis.action != null)
                turnAxis.action.performed -= TurnAction;
        }

        private void FixedUpdate()
        {
            // player.Move(moveAxis.action.ReadValue<Vector2>());
            player.Turn(turnAxis.action.ReadValue<Vector2>().x);
        }

        private void Update()
        {
            // player.Move(moveAxis.action.ReadValue<Vector2>());
            player.Turn(turnAxis.action.ReadValue<Vector2>().x);
        }

        private void MoveAction(InputAction.CallbackContext a)
        {
            var axis = a.ReadValue<Vector2>();
            // player.Move(axis);
        }

        private void TurnAction(InputAction.CallbackContext a)
        {
            var axis = a.ReadValue<Vector2>();
            player.Turn(axis.x);
        }
    }
}