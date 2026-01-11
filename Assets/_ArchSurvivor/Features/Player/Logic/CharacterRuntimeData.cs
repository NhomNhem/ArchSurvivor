namespace _ArchSurvivor.Features.Player.Logic {
    public class CharacterRuntimeData {
        public string Id { get; }
        public string Name { get; }
        public float BaseHP { get; }
        public float Defense { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }
        public float AttackSpeed { get; }
        public float Damage { get; }
        public float CritRate { get; }
        public float CritDmg { get; }
        public float PickupRange { get; }

        public float AttackLockDuration => 1f / AttackSpeed * 0.5f;

        public CharacterRuntimeData(string id, string name, float baseHp, float defense, float moveSpeed, 
            float attackRange, float attackSpeed, float damage, float critRate, float critDmg, float pickupRange) {
            Id = id;
            Name = name;
            BaseHP = baseHp;
            Defense = defense;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackSpeed = attackSpeed;
            Damage = damage;
            CritRate = critRate;
            CritDmg = critDmg;
            PickupRange = pickupRange;
        }
    }
}