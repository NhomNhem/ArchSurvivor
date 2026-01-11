using _ArchSurvivor.Features.Player.KCC;
using R3;

namespace _ArchSurvivor.Features.Player.Interfaces {
    public interface IHeroProvider {
        ReadOnlyReactiveProperty<ArchHeroController> CurrentHero { get; }
        void SetCurrentHero(ArchHeroController hero);
    }
}