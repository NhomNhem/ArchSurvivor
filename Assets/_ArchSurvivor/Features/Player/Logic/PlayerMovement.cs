using System;
using _ArchSurvivor.Core.Interfaces;
using R3;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class PlayerMovement : MonoBehaviour {
        [Header("Settings")] 
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f; 

        private ArchHeroController _heroController;
        private IInputReader _inputReader;

        [Inject]
        public void Construct(IInputReader inputReader) => _inputReader = inputReader;

        private void Awake() => _heroController = GetComponent<ArchHeroController>();

        private void Start() {
            Observable.EveryUpdate()
                .Subscribe(_ => HandleMovement())
                .RegisterTo(destroyCancellationToken);
        }

        private void HandleMovement() {
            if (_inputReader == null || _heroController == null) return;

            Vector2 input = _inputReader.MoveDirection.CurrentValue;
            Vector3 moveVector = Vector3.zero;
            Vector3 lookVector = _heroController.Motor.CharacterForward;

            if (input.sqrMagnitude > 0.001f) {
                moveVector = new Vector3(input.x, 0, input.y).normalized;
                lookVector = moveVector;
            }

            HeroCharacterInputs characterInputs = new HeroCharacterInputs {
                MoveVector = moveVector,
                LookVector = lookVector
            };

            // Set specific speed from settings if needed
            _heroController.MaxStableMoveSpeed = _moveSpeed;
            _heroController.OrientationSharpness = _rotationSpeed;

            _heroController.SetInputs(ref characterInputs);
        }
    }
}