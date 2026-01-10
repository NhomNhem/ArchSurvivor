using UnityEngine;
using VContainer;
using _ArchSurvivor.Core.Services.Data;
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
using Sisus.Init;
using VContainer.Unity;

namespace _ArchSurvivor.Features.Player.Logic {
    public class CharacterFactory {
        private readonly IDataProvider _dataProvider;
        private readonly IPlayerProvider _playerProvider;
        private readonly IObjectResolver _objectResolver;
        
        [Inject]
        public CharacterFactory(IDataProvider dataProvider, IPlayerProvider playerProvider, IObjectResolver resolver) {
            _dataProvider = dataProvider;
            _playerProvider = playerProvider;
            _objectResolver = resolver;
        }

        public void CreateCharacter(string id, GameObject prefab, Vector3 position) {
            // GET row from bakingsheet;
            var dataRow = _dataProvider.Sheets.Characters[id];
            
            // encapsulate data into runtime data
            var runtimeData = new CharacterRuntimeData(
                dataRow.Id,
                dataRow.Name,
                dataRow.ValMaxHP,
                dataRow.ValMoveSpeed
            );
            
            // Instantiate character prefab
            InitArgs.Set<ArchHeroController, CharacterRuntimeData>(runtimeData);
            
            var go = _objectResolver.Instantiate(prefab, position, Quaternion.identity);
            
            var hero = go.GetComponent<ArchHeroController>();
            
            _playerProvider.SetCurrentHero(hero);
        }
    }
}