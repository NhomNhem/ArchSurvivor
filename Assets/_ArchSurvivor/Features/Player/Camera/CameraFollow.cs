using System;
using UnityEngine;

namespace _ArchSurvivor.Features.Player.Camera {
    public class CameraFollow : MonoBehaviour {
        [Header("Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 12, -8);
        [SerializeField] private float _smoothTime = 0.2f;

        private Vector3 _currentVelocity;

        private void LateUpdate() {
            if (_target == null)
                return;
            
            Vector3 targetPosition = _target.position + _offset;
            
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                _smoothTime
            );
        }

        public void SetTarget(Transform target) => _target = target;
    }
}