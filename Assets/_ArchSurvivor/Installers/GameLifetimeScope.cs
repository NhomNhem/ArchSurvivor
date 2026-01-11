using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.Camera;
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
        [SerializeField] private HeroMovement heroMovement;
        [SerializeField] private HeroAnimation heroAnimation;
        [SerializeField] private HeroCameraFollow heroCameraFollow;

        protected override void Configure(IContainerBuilder builder) {
            // Register Components from scene
            if (_joystickInputAdapter != null) builder.RegisterComponent(_joystickInputAdapter);
            if (heroMovement != null) builder.RegisterComponent(heroMovement);
            if (heroAnimation != null) builder.RegisterComponent(heroAnimation);
            if (heroCameraFollow != null) builder.RegisterComponent(heroCameraFollow);
        }
    }
}