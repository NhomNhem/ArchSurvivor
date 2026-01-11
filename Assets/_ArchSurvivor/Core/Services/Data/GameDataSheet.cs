using System.Collections.Generic;
using Cathei.BakingSheet;
using UnityEngine.Scripting;

namespace _ArchSurvivor.Core.Services.Data {
    [Preserve]
    public class CharacterSheet : Sheet<CharacterDataRow> { }

    [Preserve]
    public class CharacterDataRow : SheetRow {
        // --- Raw Data (Cẩn thận: Đây là dữ liệu thô từ Sheets) ---
        [Preserve] public string Name { get; set; }
        [Preserve] public string Role { get; set; }
        [Preserve] public string MoveSpeed { get; set; } 
        [Preserve] public string RotationSpeed { get; set; }
        [Preserve] public string AttackRange { get; set; }
        [Preserve] public string AttackSpeed { get; set; }
        [Preserve] public string BaseHP { get; set; }
        [Preserve] public string BaseDef { get; set; }
        [Preserve] public string BaseDmg { get; set; }
        [Preserve] public string BaseCritRate { get; set; }
        [Preserve] public string BaseCritDmg { get; set; }
        [Preserve] public string PickupRange { get; set; }
        [Preserve] public string PassiveSkill { get; set; }
        [Preserve] public string PrefabName { get; set; }

        // --- Validated Data (Dùng cái này trong Gameplay) ---
        public float ValMoveSpeed => DataParser.ParseFloat(MoveSpeed, 5f, $"Hero:{Id}/MoveSpeed");
        public float ValRotationSpeed => DataParser.ParseFloat(RotationSpeed, 15f, $"Hero:{Id}/RotationSpeed");
        public float ValAttackRange => DataParser.ParseFloat(AttackRange, 2f, $"Hero:{Id}/AttackRange");
        public float ValAttackSpeed => DataParser.ParseFloat(AttackSpeed, 1f, $"Hero:{Id}/AttackSpeed");
        public float ValBaseHP => DataParser.ParseFloat(BaseHP, 100f, $"Hero:{Id}/BaseHP");
        public float ValBaseDef => DataParser.ParseFloat(BaseDef, 0f, $"Hero:{Id}/BaseDef");
        public float ValBaseDmg => DataParser.ParseFloat(BaseDmg, 10f, $"Hero:{Id}/BaseDmg");
        public float ValCritRate => DataParser.ParsePercentage(BaseCritRate, 0.05f, $"Hero:{Id}/CritRate");
        public float ValCritDmg => DataParser.ParsePercentage(BaseCritDmg, 0.5f, $"Hero:{Id}/CritDmg");
        public float ValPickupRange => DataParser.ParseFloat(PickupRange, 1f, $"Hero:{Id}/PickupRange");
    }

    [Preserve] public class CardSheet : Sheet<CardDataRow> { }

    [Preserve]
    public class CardDataRow : SheetRow {
        [Preserve] public string Name { get; set; }
        [Preserve] public string Type { get; set; }
        [Preserve] public string Rarity { get; set; }
        [Preserve] public string Description { get; set; }
        [Preserve] public string Effect { get; set; }
        
        // Bạn có thể thêm các Safe Getters cho Card tại đây nếu cần
    }

    [Preserve] public class CardEffectSheet : Sheet<CardEffectDataRow> { }

    [Preserve]
    public class CardEffectDataRow : SheetRow {
        [Preserve] public string EffectLogic { get; set; }
        [Preserve] public string Value { get; set; }

        // Ép kiểu an toàn cho Value của Card Effect
        public float ValValue => DataParser.ParseFloat(Value, 0f, $"Effect:{Id}/Value");
    }
}
