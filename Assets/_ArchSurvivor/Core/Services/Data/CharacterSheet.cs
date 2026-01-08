using Cathei.BakingSheet;
using UnityEngine;

namespace _ArchSurvivor.Core.Services.Data {
    public class CharacterSheet : Sheet<CharacterSheet.Row> {
        public class Row : SheetRow {
            // ID column is default
            public string Name { get; private set; }
            public float MoveSpeed { get; private set; }
            public float RotationSpeed { get; private set; }
            
            public float AttackRange { get; private set; }
            public float AttackSpeed { get; private set; }
            public int MaxHP { get; private set; }
            
            public string PrefabName { get; private set; }
        }
    }
}
