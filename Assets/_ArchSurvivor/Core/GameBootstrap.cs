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
using _ArchSurvivor.Features.Player.Logic;
using R3;
using UnityEngine.SceneManagement;

namespace _ArchSurvivor.Core {
    public class GameBootstrap : IAsyncStartable {
        
        private readonly string _gameplayScene;
        
        private readonly ISaveService _saveService;
        private readonly IAudioService _audioService;
        private readonly IDataProvider _dataProvider;
        private readonly CharacterFactory _characterFactory;
        private readonly UIRoot _uiRoot;
        private GameObject _playerPrefab;
        
        public GameBootstrap(ISaveService saveService, IAudioService audioService, IDataProvider dataProvider,
            string gameplayScene, CharacterFactory characterFactory,GameObject playerPrefab, UIRoot uiRoot = null) {
            _saveService = saveService;
            _audioService = audioService;
            _dataProvider = dataProvider;
            _uiRoot = uiRoot;
            _gameplayScene = gameplayScene;
            _characterFactory = characterFactory;
            _playerPrefab = playerPrefab;
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
            
            await SceneManager.LoadSceneAsync(_gameplayScene);

            _characterFactory.CreateCharacter("CHR_01", _playerPrefab, Vector3.zero);
            
#if UNITY_EDITOR
            // Auto-load Title Scene or similar if needed in dev
#endif
        }
    }
}
