using System;
using _ArchSurvivor.Common;
using _ArchSurvivor.Core.Interfaces;
using _ArchSurvivor.Features.Player.KCC;
using _ArchSurvivor.Features.Player.Logic.FSM;
using R3;
using Sisus.Init;
using UnityEngine;
using VContainer;

namespace _ArchSurvivor.Features.Player.Logic {
    public class HeroCombat : MonoBehaviour<CharacterRuntimeData, IInputReader>  {
        private CharacterRuntimeData _characterRuntimeData;
        
        private IInputReader _inputReader;
        private HeroStateMachine _stateMachine;
        private ArchHeroController _archHeroController;
        
        [SerializeField] private LayerMask enemyLayerMask;
        
        private float _lastAttackTime;
        
        protected override void Init(CharacterRuntimeData characterRuntimeData, IInputReader inputReader) {
            _characterRuntimeData = characterRuntimeData;
            _inputReader = inputReader;
        }
        private readonly Collider[] _detectionBuffer = new Collider[16];
        private Transform _currentTarget;

        private void Awake() {
            _stateMachine = GetComponent<HeroStateMachine>();
            _archHeroController = GetComponent<ArchHeroController>();
        }

        private void Update() {
            if (CanAttack()) {
                _currentTarget = FindNearestEnemy();
                ExecuteAttack();
            }
        }

        private Transform FindNearestEnemy() {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _characterRuntimeData.AttackRange, _detectionBuffer, enemyLayerMask);
            Transform nearest = null;
            float minDistanceSqr = float.MaxValue;

            for (int i = 0; i < count; i++) {
                var col = _detectionBuffer[i];
                if (col == null || !col.CompareTag(GameKeys.ArchSurvivalTagName.Enemy)) continue;

                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                if (distSqr < minDistanceSqr) {
                    minDistanceSqr = distSqr;
                    nearest = col.transform;
                }
            }
            return nearest;
        }

        private bool CanAttack() {
            bool attackPressed = _inputReader.AttackPressed.CurrentValue;
            bool isStandingStill = !_inputReader.IsMoving.CurrentValue;
            bool isReadyState = _stateMachine.CurrentState.CurrentValue == HeroStateTag.Locomotion;

            float cooldown = 1f / _characterRuntimeData.AttackSpeed;
            bool cooldownOver = Time.time >= _lastAttackTime + cooldown;
            
            return attackPressed && isStandingStill && isReadyState && cooldownOver;
        }

        private void ExecuteAttack() {
            _lastAttackTime = Time.time;
            _stateMachine.SetState(HeroStateTag.Attacking);
            
            // Xoay hướng về phía kẻ thù nếu tìm thấy
            Vector3 attackDirection = transform.forward;
            if (_currentTarget != null) {
                attackDirection = (_currentTarget.position - transform.position).normalized;
                attackDirection.y = 0;
            }

            HeroCharacterInputs inputs = new HeroCharacterInputs {
                MoveVector = Vector3.zero,
                LookVector = attackDirection
            };
            _archHeroController.SetInputs(ref inputs);

            Debug.Log($"<color=orange>[Combat]</color> Alric attacks with {_characterRuntimeData.Damage} DMG!");

            Observable.Timer(TimeSpan.FromSeconds(_characterRuntimeData.AttackLockDuration))
                .Subscribe(_ => _stateMachine.SetState(HeroStateTag.Locomotion))
                .RegisterTo(destroyCancellationToken);
        }

        private void OnDrawGizmos() {
            if (_characterRuntimeData == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _characterRuntimeData.AttackRange);
        }
    }
}