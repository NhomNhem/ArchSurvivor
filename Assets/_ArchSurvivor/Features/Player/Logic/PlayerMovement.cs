using System;
using _ArchSurvivor.Core.Interfaces;
using R3;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class PlayerMovement : MonoBehaviour {
        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f; // degrees per second
        
        private CharacterController _characterController;
        private IInputReader _inputReader;
        
        [Inject]
        public void Construct(IInputReader inputReader) => _inputReader = inputReader;
        
        private void Awake() => _characterController = GetComponent<CharacterController>();

        private void Start() {
            Observable.EveryUpdate()
                .Subscribe(_ => HandleMovement())
                .RegisterTo(destroyCancellationToken);
        }

        private void HandleMovement() {
            if (_inputReader == null) return;
            
            Vector2 input = _inputReader.MoveDirection.CurrentValue;

            if (input.sqrMagnitude > 0.001f) {
                Vector3 targetDir = new Vector3(input.x, 0, input.y).normalized;
                
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
                
                _characterController.Move(targetDir * _moveSpeed * Time.deltaTime);
            }
            
            _characterController.Move(Vector3.down * 5f * Time.deltaTime);
        }
    }
}