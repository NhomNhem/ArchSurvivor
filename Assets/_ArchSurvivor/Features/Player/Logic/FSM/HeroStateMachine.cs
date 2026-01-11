using R3;
using UnityEngine;

namespace _ArchSurvivor.Features.Player.Logic.FSM {
    public class HeroStateMachine : MonoBehaviour {
        private readonly ReactiveProperty<HeroStateTag> _currentState = new(HeroStateTag.Locomotion);
        
        public ReadOnlyReactiveProperty<HeroStateTag> CurrentState => _currentState;

        public void SetState(HeroStateTag newState) {
            if (_currentState.Value == newState) return;
            
            _currentState.Value = newState;
            Debug.Log($"[FSM] Alric changed state to: {_currentState.Value}");
        }
    }
}