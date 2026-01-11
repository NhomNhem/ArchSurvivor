using _ArchSurvivor.Core.Interfaces;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;
using System;

namespace _ArchSurvivor.Core.Services.Input {
    public class InputService : IInputReader, IStartable, IDisposable {
        private readonly InputSystem_Actions _actions;
        private readonly ReactiveProperty<Vector2> _moveDirection = new(Vector2.zero);
        private readonly ReactiveProperty<bool> _isMoving = new(false);
        private readonly ReactiveProperty<bool> _attackPressed = new(false);

        public ReadOnlyReactiveProperty<Vector2> MoveDirection => _moveDirection;
        public ReadOnlyReactiveProperty<bool> IsMoving => _isMoving;
        public ReadOnlyReactiveProperty<bool> AttackPressed => _attackPressed;

        public InputService() {
            _actions = new InputSystem_Actions();
        }

        public void Start() {
            _actions.Enable();
            
            // Subscribe to Move action
            _actions.Player.Move.performed += OnMove;
            _actions.Player.Move.canceled += OnMove;
        }

        private void OnMove(InputAction.CallbackContext context) {
            var input = context.ReadValue<Vector2>();
            _moveDirection.Value = input;
            _isMoving.Value = input != Vector2.zero;
        }

        public void SetJoystickInput(Vector2 input) {
            // This allows the Joystick UI to inject values if it's not handled by the Input System directly
            _moveDirection.Value = input;
            _isMoving.Value = input != Vector2.zero;
        }

        public void SetAttackInput(bool isPressed) {
            _attackPressed.Value = isPressed;
        }
        
        public void Dispose() {
            _actions.Disable();
            _actions.Dispose();
            _moveDirection.Dispose();
            _isMoving.Dispose();
        }
    }
}
