using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _ArchSurvivor.Core.UI {
    [ExecuteInEditMode]
    public class UIToolkitSafeArea : MonoBehaviour {
        [Tooltip("UI Document that contains the UXML hierarchy")] [SerializeField]
        private UIDocument uiDocument;

        [Tooltip("Color for the border areas. Use a transparent color to show the background")] [SerializeField]
        private Color borderColor = Color.black;

        [Tooltip("Name of top-level element container. Leave empty to use the root element.")] [SerializeField]
        private string containerElementName = "SafeAreaContainer";

        [Tooltip("Percentage multiplier for safe area distance")] [Range(0f, 1f)] [SerializeField]
        private float safeAreaMultiplier = 1f;

        [Tooltip("Show debug messages in the console")] [SerializeField]
        private bool debugMode = false;

        private VisualElement _root;
        private float _leftBorder;
        private float _rightBorder;
        private float _topBorder;
        private float _bottomBorder;

        public VisualElement RootElement => _root;
        public float LeftBorder => _leftBorder;
        public float RightBorder => _rightBorder;
        public float TopBorder => _topBorder;
        public float BottomBorder => _bottomBorder;

        public float Multiplier {
            get => safeAreaMultiplier;
            set => safeAreaMultiplier = value;
        }

        private void Awake() {
            Initialize();
        }

        private void Initialize() {
            if (uiDocument == null || uiDocument.rootVisualElement == null) {
                Debug.LogWarning("uiDocument is null");
                return;
            }

            if (string.IsNullOrEmpty(containerElementName))
                _root = uiDocument.rootVisualElement;
            else
                _root = uiDocument.rootVisualElement.Q<VisualElement>(containerElementName);

            if (_root == null) {
                if (debugMode) Debug.LogWarning("uiDocument is null");
                return;
            }
            
            _root.RegisterCallback<GeometryChangedEvent>(evt => OnGeometryChanged(evt));
            
            ApplySafeArea();
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplySafeArea();

        private void OnValidate() {
            ApplySafeArea();
        }

        private void ApplySafeArea() {
            if (_root == null)
                return;

            Rect safeArea = Screen.safeArea;

            // Calculate borders based on safe area rect
            _leftBorder = safeArea.x;
            _rightBorder = Screen.width - safeArea.xMax;
            _topBorder = Screen.height - safeArea.yMax;
            _bottomBorder = safeArea.y;


            // Set border widths regardless of orientation
            _root.style.paddingTop = _topBorder * safeAreaMultiplier;
            _root.style.paddingBottom = _bottomBorder * safeAreaMultiplier;
            _root.style.paddingLeft = _leftBorder * safeAreaMultiplier;
            _root.style.paddingRight = _rightBorder * safeAreaMultiplier;


            // Apply border color
           

            if (debugMode) Debug.Log($"[SafeAreaBorder] Applied Safe Area | Screen Orientation: {Screen.orientation} | Left: {LeftBorder}, Right: {RightBorder}, Top: {TopBorder}, Bottom: {BottomBorder}");
        }
    }
}