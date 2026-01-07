using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace _ArchSurvivor.Core.UI {
    public class UIRoot : MonoBehaviour {
        [SerializeField] private UIDocument _mainDocument;
        [SerializeField] private PanelSettings _globalPanelSettings;

        public VisualElement Root => _mainDocument.rootVisualElement;

        private void Awake() {
            if (_mainDocument == null) _mainDocument = GetComponent<UIDocument>();
        }

        public void AddToHUD(VisualElement element) {
            Root.Q("hud-container")?.Add(element);
        }

        public void ShowPopup(VisualElement element) {
            var overlay = Root.Q("popup-overlay");
            overlay.style.display = DisplayStyle.Flex;
            overlay.Add(element);
        }
    }
}
