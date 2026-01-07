using System.Threading;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer.Unity;
using UnityEngine;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.UI;
using _ArchSurvivor.Core.Services.Data;
using _ArchSurvivor.Common.Utilities;

namespace _ArchSurvivor.Core {
    public class GameBootstrap : IAsyncStartable {
        private readonly ISaveService _saveService;
        private readonly IAudioService _audioService;
        private readonly IDataProvider _dataProvider;
        private readonly UIRoot _uiRoot;

        public GameBootstrap(
            ISaveService saveService, 
            IAudioService audioService, 
            IDataProvider dataProvider,
            UIRoot uiRoot) {
            _saveService = saveService;
            _audioService = audioService;
            _dataProvider = dataProvider;
            _uiRoot = uiRoot;
        }

        public async UniTask StartAsync(CancellationToken cancellation) {
            GameLog.Info("ArchSurvivor Initializing...");
            
            // 1. Load Game Data
            await _dataProvider.LoadAsync(cancellation);

            // 2. Initialize basic settings
            var musicVol = _saveService.Load("Settings_MusicVol", 0.8f);
            _audioService.SetBGMVolume(musicVol);

            // 3. Initialize UI
            if (_uiRoot != null) {
                GameLog.Info("Global UI Root initialized.");
            }
            
            GameLog.Info("Initialized. Ready for Battle!");

#if UNITY_EDITOR
            // Auto-load Title Scene or similar if needed in dev
#endif
        }
    }
}
