using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.Services.Input;
using Sisus.Init;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class JoystickInputAdapter : MonoBehaviour<IInputReader> {
        [Header("Juice Settings")]
        [SerializeField] private RectTransform _directionIndicator;
        [SerializeField] private CanvasGroup _indicatorCanvasGroup;
        [SerializeField] private float _fadeSpeed = 10f;
        [SerializeField] private VariableJoystick _joystickUI;
        [SerializeField, Range(0f, 1f)] private float _deadZone = 0.1f;

        private InputService _inputService;
        private IInputReader _inputReader;
        
        protected override void Init(IInputReader dependency) {
            _inputReader = dependency;
            _inputService = dependency as InputService;
        }
        
        
        private void Start() {
            if (_joystickUI == null)
                _joystickUI = GetComponentInChildren<VariableJoystick>();
            
            if (_indicatorCanvasGroup != null)
                _indicatorCanvasGroup.alpha = 0f;
        }

        private void Update() {
            if (_joystickUI == null) return;

            Vector2 input = _joystickUI.Direction;
            float magnitude = input.magnitude;
            
            if (magnitude < _deadZone) {
                input = Vector2.zero;
            }
            
            _inputService.SetJoystickInput(input);
            HandleVisualFeedback(input, magnitude);
        }

        private void HandleVisualFeedback(Vector2 input, float magnitude) {
            if (_directionIndicator == null) return;

            // 1. Handle Alpha (Fade in/out)
            float targetAlpha = (magnitude > _deadZone) ? 1f : 0f;
            if (_indicatorCanvasGroup != null) {
                _indicatorCanvasGroup.alpha = Mathf.Lerp(_indicatorCanvasGroup.alpha, targetAlpha, Time.deltaTime * _fadeSpeed);
            }

            // 2. Handle Rotation
            if (magnitude > _deadZone) {
                float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
                _directionIndicator.rotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        private void OnDestroy() {
            // Logic is now managed by InputService
        }

       
    }
}