
using _ArchSurvivor.Features.Player.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
using R3;

namespace _ArchSurvivor.Core.Services {
    public class HeroProvider :IHeroProvider {
        private readonly ReactiveProperty<ArchHeroController> _currentHero = new ReactiveProperty<ArchHeroController>(null);
        
        public ReadOnlyReactiveProperty<ArchHeroController> CurrentHero => _currentHero;
        public void SetCurrentHero(ArchHeroController hero) => _currentHero.Value = hero;
    }
}