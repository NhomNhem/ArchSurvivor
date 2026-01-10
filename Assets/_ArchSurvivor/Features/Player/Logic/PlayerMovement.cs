using System;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
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
        private IPlayerProvider _playerProvider;

        [Inject]
        public void Construct(IInputReader inputReader, IPlayerProvider playerProvider) {
            _inputReader = inputReader;
            _playerProvider = playerProvider;
        }

        private void Awake() => _heroController = GetComponent<ArchHeroController>();

        private void Start() {
            Observable.EveryUpdate()
                .Subscribe(_ => HandleMovement())
                .RegisterTo(destroyCancellationToken);
            
            _playerProvider.CurrentHero
                .Where(p => p != null)
                .Subscribe(player => {
                    _heroController = player;
                    Debug.Log("PlayerMovement: HeroController assigned from PlayerProvider.");
                })
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

            // Senior Tip: Chúng ta không ghi đè tốc độ từ đây nữa. 
            // Tốc độ đã được nạp từ Google Sheets vào ArchHeroController thông qua InitArgs rồi.
            // _heroController.MaxStableMoveSpeed = _moveSpeed; // Dòng này gây lỗi vì biến đã bị xóa
            _heroController.OrientationSharpness = _rotationSpeed;

            _heroController.SetInputs(ref characterInputs);
        }
    }
}