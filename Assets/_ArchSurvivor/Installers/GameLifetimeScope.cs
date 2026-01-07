using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.Logic;
using _ArchSurvivor.Features.Player.Visuals;
using Animancer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _ArchSurvivor.Installers {
    public class GameLifetimeScope : LifetimeScope {
        [Header("Scene References")]
        [SerializeField] private JoystickInputAdapter _joystickInputAdapter;

        protected override void Configure(IContainerBuilder builder) {
            if (_joystickInputAdapter == null) return;
            
            builder.RegisterComponent(_joystickInputAdapter).As<IInputReader>();
            
            builder.RegisterBuildCallback(container => {
                var player = FindFirstObjectByType<PlayerAnimation>();

                if (player != null) {
                    container.Inject(player);
                    Debug.Log("Injected PlayerAnimation dependencies.");
                }
                else {
                    Debug.Log("No PlayerAnimation found.");
                }
            });
        }
    }
}