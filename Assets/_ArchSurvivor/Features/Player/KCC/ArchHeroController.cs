using _ArchSurvivor.Features.Player.Logic;
using KinematicCharacterController;
using Sisus.Init;
using UnityEngine;

namespace _ArchSurvivor.Features.Player.KCC {
    public struct HeroCharacterInputs {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    /// <summary>
    /// Senior Tip: MonoBehaviour<T> từ Sisus.Init giúp nhận dữ liệu TRƯỚC khi Awake.
    /// Chúng ta dùng dữ liệu này để cấu hình các thông số vật lý của KCC.
    /// </summary>
    public class ArchHeroController : MonoBehaviour<CharacterRuntimeData>, ICharacterController {
        [Header("References")]
        public KinematicCharacterMotor Motor;

        [Header("Movement Settings (Static)")] 
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 15f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;
        public Vector3 Gravity = new Vector3(0, -20f, 0);

        // Dữ liệu từ Google Sheets sẽ đổ vào đây
        private CharacterRuntimeData _data;
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;

        public CharacterRuntimeData Data => _data;

        // Bước 1: Nhận dữ liệu khởi tạo
        protected override void Init(CharacterRuntimeData data) {
            _data = data;
            Debug.Log($"[KCC] {data.Name} initialized with Speed: {data.MoveSpeed}");
        }

        private void Start() {
            if (Motor != null) {
                Motor.CharacterController = this;
            }
        }

        public void SetInputs(ref HeroCharacterInputs inputs) {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        #region KCC Implementation
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
            if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f) {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
            
            // Keep the character upright
            Vector3 currentUp = (currentRotation * Vector3.up);
            currentRotation = Quaternion.FromToRotation(currentUp, Vector3.up) * currentRotation;
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
            // Sử dụng MoveSpeed từ dữ liệu đã nạp (Senior Style)
            float targetSpeed = _data?.MoveSpeed ?? 5f;

            if (Motor.GroundingStatus.IsStableOnGround) {
                float currentVelocityMagnitude = currentVelocity.magnitude;
                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;
                Vector3 targetMovementVelocity = _moveInputVector * targetSpeed;

                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            else {
                if (_moveInputVector.sqrMagnitude > 0f) {
                    Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;
                    Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    if (currentVelocityOnInputsPlane.magnitude < targetSpeed) {
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, targetSpeed);
                        addedVelocity = newTotal - currentVelocityOnInputsPlane;
                    } else {
                        if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f) {
                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                        }
                    }
                    currentVelocity += addedVelocity;
                }

                currentVelocity += Gravity * deltaTime;
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public void AfterCharacterUpdate(float deltaTime) { }
        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        #endregion
    }
}
