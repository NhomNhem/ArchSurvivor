using _ArchSurvivor.Core.Services.Audio;
using _ArchSurvivor.Core.Services.Data;
using _ArchSurvivor.Core.Services.Input;
using _ArchSurvivor.Core.UI;
using _ArchSurvivor.Core;
using _ArchSurvivor.Core.Interfaces;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope {
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private UIRoot uiRootPrefab;

    protected override void Configure(IContainerBuilder builder) {
        // Global Services
        builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
        builder.Register<DataService>(Lifetime.Singleton).As<IDataProvider>();
        
        if (audioMixer != null) {
            builder.RegisterInstance(audioMixer);
            builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
        }

        builder.Register<InputService>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .As<IInputReader>();
        
        // UI
        if (uiRootPrefab != null) {
            builder.RegisterComponentInNewPrefab(uiRootPrefab, Lifetime.Singleton)
                .UnderTransform(transform);
        }
        
        // Entry Points
        builder.RegisterEntryPoint<GameBootstrap>();
    }
}

