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
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerAnimation _playerAnimation;

        protected override void Configure(IContainerBuilder builder) {
            if (_joystickInputAdapter != null) builder.RegisterComponent(_joystickInputAdapter);
            if (_playerMovement != null) builder.RegisterComponent(_playerMovement);
            if (_playerAnimation != null) builder.RegisterComponent(_playerAnimation);
        }
    }
}