using _ArchSurvivor.Common.Utilities;
using _ArchSurvivor.Core.Services.Audio;
using _ArchSurvivor.Core.Services.Data;
using _ArchSurvivor.Core.Services.Input;
using _ArchSurvivor.Core.UI;
using _ArchSurvivor.Core;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.Services;
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.Logic;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope {
    
    [Header("Global References")]
    [SerializeField] private Udar.SceneManager.SceneField gameplayScene;
    
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private UIRoot uiRootPrefab;
    [SerializeField] private GameObject playerPrefab;

    protected override void Configure(IContainerBuilder builder) {
        // Global Services
        builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
        builder.Register<DataService>(Lifetime.Singleton).As<IDataProvider>();
        
        // Register Factories & Services
        builder.Register<CharacterFactory>(Lifetime.Singleton);
        
        builder.Register<PlayerProvider>(Lifetime.Singleton).As<IPlayerProvider>();
        
        if (audioMixer != null) {
            builder.RegisterInstance(audioMixer);
            builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
        }
        else { 
            builder.Register(container => new AudioService(null), Lifetime.Singleton).As<IAudioService>();
            GameLog.Warning("AudioMixer reference is missing in ProjectLifetimeScope. AudioService will use default settings.");
        }
        
        builder.Register<InputService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        
        // UI
        if (uiRootPrefab != null) {
            builder.RegisterComponentInNewPrefab(uiRootPrefab, Lifetime.Singleton).UnderTransform(transform);
        }
        
        // Entry Points
        builder.RegisterEntryPoint<GameBootstrap>()
            .WithParameter("gameplayScene", gameplayScene.Name)
            .WithParameter("playerPrefab", playerPrefab)
            .WithParameter("uiRoot", (UIRoot)null);
    }
}

