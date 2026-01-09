using System;
using KinematicCharacterController;
using UnityEngine;

namespace _ArchSurvivor.Features.Player.Logic {
    public struct HeroCharacterInputs {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public class ArchHeroController : MonoBehaviour, ICharacterController {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")] 
        public float MaxStableMoveSpeed = 5f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 15f;

        [Header("Air Movement")] 
        public float MaxAirMoveSpeed = 5f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Misc")] 
        public Vector3 Gravity = new Vector3(0, -20f, 0);

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;

        private void Start() {
            if (Motor != null) {
                Motor.CharacterController = this;
            }
        }

        public void SetInputs(ref HeroCharacterInputs inputs) {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

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
            // Ground movement
            if (Motor.GroundingStatus.IsStableOnGround) {
                float currentVelocityMagnitude = currentVelocity.magnitude;
                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                // Reorient velocity on slope
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                // Calculate target velocity
                Vector3 targetMovementVelocity = _moveInputVector * MaxStableMoveSpeed;

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            // Air movement
            else {
                if (_moveInputVector.sqrMagnitude > 0f) {
                    Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;
                    Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed) {
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
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
    }
}
