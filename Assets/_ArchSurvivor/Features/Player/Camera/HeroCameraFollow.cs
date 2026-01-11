using System;
using _ArchSurvivor.Core.Services;
using _ArchSurvivor.Features.Player.Interfaces;
using R3;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Camera {
    public class HeroCameraFollow : MonoBehaviour {
        
        [SerializeField] private CinemachineCamera _vCamera;
        
        private IHeroProvider _heroProvider;
        
        [Inject]
        public void Construct(IHeroProvider heroProvider) {
            // Constructor logic if needed
            _heroProvider = heroProvider;
        }

        private void Start() {
            _heroProvider.CurrentHero
                .Where(hero => hero != null)
                .Subscribe(hero => {
                    if (_vCamera == null) {
                        Debug.LogWarning("CinemachineCamera reference is missing.");
                        return;
                    }
                    
                    _vCamera.Follow = hero.transform;
                    _vCamera.LookAt = hero.transform;
                    Debug.Log("Find hero for camera follow");
                })
                .RegisterTo(destroyCancellationToken);
        }
    }
}