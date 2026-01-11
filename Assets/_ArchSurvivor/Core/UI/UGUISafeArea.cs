using System;
using UnityEngine;

namespace _ArchSurvivor.Core.UI {
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class UGUISafeArea : MonoBehaviour {
        private RectTransform _rectTransform;

        private void Awake() => _rectTransform = GetComponent<RectTransform>();
        private void Update() => ApplySafeArea();

        private void ApplySafeArea() {
            Rect safeArea = Screen.safeArea;
            
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}