using System;
using _ArchSurvivor.Core.Interfaces;
using Animancer;
using R3;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Visuals {
    public class PlayerAnimation : MonoBehaviour {
        [Header("Components")]
        [SerializeField] private AnimancerComponent animancer;

        [Header("Animations")] 
        [SerializeField] private ClipTransition idle;
        [SerializeField] private ClipTransition run;
        
        private IInputReader _inputReader;
        
        [Inject]
        public void Construct(IInputReader inputReader) => _inputReader = inputReader;

        private void Start() {
            if (animancer == null) animancer = GetComponent<AnimancerComponent>();
            
            animancer.Play(idle);

            _inputReader.IsMoving
                .DistinctUntilChanged()
                .Subscribe(isMoving => {
                    if (isMoving)
                        animancer.Play(run);
                    else
                        animancer.Play(idle);
                })
                .RegisterTo(destroyCancellationToken);
        }
    }
}