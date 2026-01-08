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
using R3;
using UnityEngine.SceneManagement;

namespace _ArchSurvivor.Core {
    public class GameBootstrap : IAsyncStartable {
        
        private readonly string _gameplayScene;
        
        private readonly ISaveService _saveService;
        private readonly IAudioService _audioService;
        private readonly IDataProvider _dataProvider;
        private readonly UIRoot _uiRoot;

        public GameBootstrap(ISaveService saveService, IAudioService audioService, IDataProvider dataProvider, string gameplayScene, UIRoot uiRoot = null) {
            _saveService = saveService;
            _audioService = audioService;
            _dataProvider = dataProvider;
            _uiRoot = uiRoot;
            _gameplayScene = gameplayScene;
        }

        public async UniTask StartAsync(CancellationToken cancellation) {
            GameLog.Info("ArchSurvivor Initializing...");
            ObservableSystem.DefaultFrameProvider = UnityFrameProvider.Update;

            // 1. Load Game Data
            await _dataProvider.LoadAsync(cancellation);
            
            // 2. Initialize basic settings
            if (_audioService != null) {
                var musicVol = _saveService.Load("Settings_MusicVol", 0.8f);
                _audioService.SetBGMVolume(musicVol);
            }

            // 3. Initialize UI
            if (_uiRoot != null) {
                GameLog.Info("Global UI Root initialized.");
            }
            
            GameLog.Info("Initialized. Ready for Battle!");
            
            SceneManager.LoadScene(_gameplayScene);

#if UNITY_EDITOR
            // Auto-load Title Scene or similar if needed in dev
#endif
        }
    }
}
