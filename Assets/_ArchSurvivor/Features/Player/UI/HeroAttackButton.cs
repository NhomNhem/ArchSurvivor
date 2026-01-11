using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Core.Services.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace _ArchSurvivor.Features.Player.UI {
    /// <summary>
    /// A simple uGUI script to handle manual attack button input.
    /// Injects state into InputService via IInputReader.
    /// </summary>
    public class HeroAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        private InputService _inputService;

        [Inject]
        public void Construct(IInputReader inputReader) {
            _inputService = inputReader as InputService;
        }

        public void OnPointerDown(PointerEventData eventData) {
            _inputService?.SetAttackInput(true);
            
            // TODO: Add visual feedback (scale down, change color, etc.)
        }

        public void OnPointerUp(PointerEventData eventData) {
            _inputService?.SetAttackInput(false);
            
            // TODO: Reset visual feedback
        }
    }
}
