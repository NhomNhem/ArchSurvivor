using UnityEngine;
using VContainer;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.Services.Data;
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
using _ArchSurvivor.Features.Player.Visuals;
using Sisus.Init;
using VContainer.Unity;

namespace _ArchSurvivor.Features.Player.Logic {
    public class CharacterFactory {
        private readonly IDataProvider _dataProvider;
        private readonly IHeroProvider _heroProvider;
        private readonly IObjectResolver _objectResolver;
        
        [Inject]
        public CharacterFactory(IDataProvider dataProvider, IHeroProvider heroProvider, IObjectResolver resolver) {
            _dataProvider = dataProvider;
            _heroProvider = heroProvider;
            _objectResolver = resolver;
        }

        public void CreateCharacter(string id, GameObject prefab, Vector3 position) {

            if (prefab == null) {
                Debug.LogWarning("prefab is null");
                return;
            }
            
            // GET row from bakingsheet;
            var dataRow = _dataProvider.Sheets.Characters[id];
            
            // encapsulate data into runtime data
            var runtimeData = new CharacterRuntimeData(
                dataRow.Id,
                dataRow.Name,
                dataRow.ValBaseHP,
                dataRow.ValBaseDef,
                dataRow.ValMoveSpeed,
                dataRow.ValAttackRange,
                dataRow.ValAttackSpeed,
                dataRow.ValBaseDmg,
                dataRow.ValCritRate,
                dataRow.ValCritDmg,
                dataRow.ValPickupRange
            );
            
            InitArgs.Set<IArgs<CharacterRuntimeData>, CharacterRuntimeData>(runtimeData);
            
            var go = Object.Instantiate(prefab, position, Quaternion.identity);
            
            _objectResolver.InjectGameObject(go);
            
            var hero = go.GetComponent<ArchHeroController>();
            
            _heroProvider.SetCurrentHero(hero);
        }
    }
}