using System;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.Logic;
using _ArchSurvivor.Features.Player.Logic.FSM;
using Animancer;
using R3;
using Sisus.Init;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Visuals {
    public class HeroAnimation : MonoBehaviour<CharacterRuntimeData> {
        [Header("Components")]
        [SerializeField] private AnimancerComponent animancer;

        [Header("Animations")] 
        [SerializeField] private ClipTransition idle;
        [SerializeField] private ClipTransition run;
        [SerializeField] private ClipTransition attack;
        
        private HeroStateMachine _stateMachine;
        private IInputReader _inputReader;
        private CharacterRuntimeData _characterRuntimeData;
        
        protected override void Init(CharacterRuntimeData characterRuntimeData) {
            _characterRuntimeData = characterRuntimeData;
        }
        
        [Inject]
        public void Construct(IInputReader inputReader) => _inputReader = inputReader;

        private void Awake() {
            _stateMachine = GetComponent<HeroStateMachine>();
        }
        
        private void Start() {
            _stateMachine.CurrentState
                .Subscribe(state => {
                    switch (state) {
                        case HeroStateTag.Locomotion:
                            UpdateLocomotionAnimation(_inputReader.IsMoving.CurrentValue);
                            break;
                        case HeroStateTag.Attacking:
                            animancer.Play(attack);
                            break;
                    }
                })
                .RegisterTo(destroyCancellationToken);
            
            _inputReader.IsMoving
                .Where(_ => _stateMachine.CurrentState.CurrentValue == HeroStateTag.Locomotion)
                .Subscribe(UpdateLocomotionAnimation)
                .RegisterTo(destroyCancellationToken);
        }

        private void UpdateLocomotionAnimation(bool isMoving) => animancer.Play(isMoving ? run : idle);
    }
}