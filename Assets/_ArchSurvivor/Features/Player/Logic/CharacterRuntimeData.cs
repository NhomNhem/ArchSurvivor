namespace _ArchSurvivor.Features.Player.Logic {
    public class CharacterRuntimeData {
        public string Id {get;}
        public string Name {get;}
        public float MaxHP {get;}
        public float MoveSpeed {get;}
        
        public CharacterRuntimeData(string id, string name, float maxHP, float moveSPeed) {
            Id = id;
            Name = name;
            MaxHP = maxHP;
            MoveSpeed = moveSPeed;
        }
    }
}