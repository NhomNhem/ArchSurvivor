using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.Services.Input;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    /// <summary>
    /// Adapter to bridge Ultimate Joystick asset with our custom InputService.
    /// This allows us to use high-quality 3rd party UI while keeping gameplay logic decoupled.
    /// </summary>
    public class UltimateJoystickAdapter : MonoBehaviour {
        [Header("Settings")]
        [SerializeField] private UltimateJoystick _joystick;
        
        private InputService _inputService;

        [Inject]
        public void Construct(IInputReader inputReader) {
            // We cast to InputService to use the internal SetJoystickInput method
            _inputService = inputReader as InputService;
            
            if (_inputService == null) {
                Debug.LogError("UltimateJoystickAdapter requires InputService as IInputReader implementation.");
            }
        }

        private void Start() {
            if (_joystick == null) {
                _joystick = GetComponent<UltimateJoystick>();
                
                if (_joystick == null) {
                    Debug.LogWarning("UltimateJoystick reference is missing in UltimateJoystickAdapter!");
                }
            }
        }

        private void Update() {
            if (_joystick == null) return;

            // Ultimate Joystick provides properties for axis values directly
            Vector2 direction = new Vector2(_joystick.HorizontalAxis, _joystick.VerticalAxis);
            
            // Inject into our centralized input system
            _inputService.SetJoystickInput(direction);
        }
    }
}
