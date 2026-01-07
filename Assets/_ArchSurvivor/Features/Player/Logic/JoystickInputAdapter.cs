using _ArchSurvivor.Core.Services.Input;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class JoystickInputAdapter : MonoBehaviour {
        [Header("Dependencies")]
        [SerializeField] private VariableJoystick _joystickUI;
        [SerializeField] private float _deadZone = 0.1f;

        private InputService _inputService;

        [Inject]
        public void Construct(InputService inputService) {
            _inputService = inputService;
        }

        private void Start() {
            if (_joystickUI == null)
                _joystickUI = GetComponentInChildren<VariableJoystick>();
        }

        private void Update() {
            if (_joystickUI == null) return;

            Vector2 input = _joystickUI.Direction;
            
            if (input.sqrMagnitude < _deadZone * _deadZone) {
                input = Vector2.zero;
            }
            
            _inputService.SetJoystickInput(input);
        }

        private void OnDestroy() {
            // Logic is now managed by InputService
        }
    }
}