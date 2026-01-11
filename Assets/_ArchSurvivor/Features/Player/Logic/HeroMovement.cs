using System;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
using _ArchSurvivor.Features.Player.Logic.FSM;
using R3;
using Sisus.Init;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class HeroMovement : MonoBehaviour<CharacterRuntimeData> {
        [Header("Settings")] 
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f; 

        private ArchHeroController _heroController;
        private IInputReader _inputReader;
        private IHeroProvider _heroProvider;
        
        private CharacterRuntimeData _characterRuntimeData;
        
        private HeroStateMachine _heroStateMachine;

        [Inject]
        public void Construct(IInputReader inputReader, IHeroProvider heroProvider) {
            _inputReader = inputReader;
            _heroProvider = heroProvider;
        }

        protected override void Init(CharacterRuntimeData characterRuntimeData) {
            _characterRuntimeData = characterRuntimeData;
        }

        private void Awake() {
            _heroController = GetComponent<ArchHeroController>();
            _heroStateMachine = GetComponent<HeroStateMachine>();
        }

        private void Start() {
            Observable.EveryUpdate()
                .Subscribe(_ => HandleMovement())
                .RegisterTo(destroyCancellationToken);
            
            _heroProvider.CurrentHero
                .Where(p => p != null)
                .Subscribe(player => {
                    _heroController = player;
                    Debug.Log("PlayerMovement: HeroController assigned from PlayerProvider.");
                })
                .RegisterTo(destroyCancellationToken);
        }

        private void HandleMovement() {
            if (_inputReader == null || _heroController == null) return;

            if (_heroStateMachine != null && _heroStateMachine.CurrentState.CurrentValue != HeroStateTag.Locomotion) {
                HeroCharacterInputs emptyInputs = new HeroCharacterInputs {
                    MoveVector = Vector3.zero,
                    LookVector = _heroController.Motor.CharacterForward
                };
                _heroController.SetInputs(ref emptyInputs);
                return;
            }

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