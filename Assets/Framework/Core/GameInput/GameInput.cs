using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework.Extensions.InputSystem
{
    public enum InputButton
    {
        LeftPrimary = 0,
        RightPrimary = 1,
        LeftSecondary = 2,
        RightSecondary = 3,
        LeftGrip = 4,
        RightGrip = 5,
        LeftTrigger = 6,
        RightTrigger = 7,
    }

    [Serializable]
    public class GameButtons
    {
        private readonly Dictionary<InputButton, bool> _buttons = new();

        public void Set(InputButton button, bool value) => _buttons[button] = value;

        public bool IsSet(InputButton button) => _buttons.TryGetValue(button, out var value) && value;
    }

    /// <summary>
    /// InputManager 使用指南:
    /// 两种输入范式:
    /// 1. 监听回调，用于一次性事件/表现
    ///    InputManager.Instance.InputButton += OnButtonPressed;
    /// 2. 帧内轮询，用于状态判断
    ///    if (InputManager.Instance.IsHeld(InputButton.LeftPrimary)) { ... }
    ///    if (InputManager.Instance.Move.x > 0) { ... }
    /// </summary>
    public static class GameInput
    {
        private static GameInputActions _inputActions;

        // For local input
        private static GameButtons _heldButtons;

        // Button events
        public static event Action<InputButton> ButtonPressedEvent;
        public static event Action<InputButton> ButtonReleasedEvent;

        // Input values
        public static Vector2 Move { get; private set; }

        public static Vector2 Turn { get; private set; }

        public static float LeftGrip { get; private set; }

        public static float RightGrip { get; private set; }

        public static float LeftTrigger { get; private set; }

        public static float RightTrigger { get; private set; }

        public static bool IsHeld(InputButton button) => _heldButtons.IsSet(button);

        internal static void Initialize()
        {
            _inputActions = new GameInputActions();
            _inputActions.Enable();

            _heldButtons = new GameButtons();

            BindButton(
                InputButton.LeftPrimary,
                _inputActions.Hand.PrimaryPressedL,
                _inputActions.Hand.PrimaryReleasedL
            );
            BindButton(
                InputButton.RightPrimary,
                _inputActions.Hand.PrimaryPressedR,
                _inputActions.Hand.PrimaryReleasedR
            );

            BindButton(
                InputButton.LeftSecondary,
                _inputActions.Hand.SecondaryPressedL,
                _inputActions.Hand.SecondaryReleasedL
            );
            BindButton(
                InputButton.RightSecondary,
                _inputActions.Hand.SecondaryPressedR,
                _inputActions.Hand.SecondaryReleasedR
            );

            BindButton(InputButton.LeftGrip, _inputActions.Hand.GripPressedL, _inputActions.Hand.GripReleasedL);
            BindButton(InputButton.RightGrip, _inputActions.Hand.GripPressedR, _inputActions.Hand.GripReleasedR);

            BindButton(
                InputButton.LeftTrigger,
                _inputActions.Hand.TriggerPressedL,
                _inputActions.Hand.TriggerReleasedL
            );
            BindButton(
                InputButton.RightTrigger,
                _inputActions.Hand.TriggerPressedR,
                _inputActions.Hand.TriggerReleasedR
            );
        }

        private static void BindButton(InputButton button, InputAction pressedAction, InputAction releasedAction)
        {
            pressedAction.performed += _ =>
            {
                _heldButtons.Set(button, true);
                ButtonPressedEvent?.Invoke(button);
            };

            releasedAction.performed += _ =>
            {
                _heldButtons.Set(button, false);
                ButtonReleasedEvent?.Invoke(button);
            };
        }

        internal static void Dispose()
        {
            if (_inputActions is not null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
        }

        internal static void Update()
        {
            Move = _inputActions.Player.Move.ReadValue<Vector2>();
            Turn = _inputActions.Player.Look.ReadValue<Vector2>();

            LeftGrip = _inputActions.Hand.GripAxisL.ReadValue<float>();
            RightGrip = _inputActions.Hand.GripAxisR.ReadValue<float>();
            LeftTrigger = _inputActions.Hand.TriggerAxisL.ReadValue<float>();
            RightTrigger = _inputActions.Hand.TriggerAxisR.ReadValue<float>();
        }
    }
}