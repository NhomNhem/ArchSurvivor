using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MasterStylizedProjectile
{
    public class Bullet:MonoBehaviour
    {
        public float Speed = 5;
        public ParticleSystem OnHitEffect;
        public AudioClip bulletClip;
        public AudioClip onHitClip;

        public bool isTargeting;
        public Transform target;
        public float rotSpeed = 0;
        [Tooltip("是否平射，如果为true，子弹在y轴方向的速度为0，且不能朝上下旋转")]
        public bool isFlatShoot = false;
        
        private float initialYPosition;
        
        private void Start()
        {
            // 存储初始y坐标（用于平射时保持y坐标不变）
            initialYPosition = transform.position.y;
            
            if (bulletClip != null)
            {
                var audio = gameObject.AddComponent<AudioSource>();
                audio.clip = bulletClip;
                audio.Play();
            }
        }
        private void Update()
        {
            if (isTargeting == true && target != null)
            {
                Vector3 targetDirection = target.position - transform.position;
                
                // 如果平射，限制旋转只能在xz平面（不能上下旋转）
                if (isFlatShoot)
                {
                    targetDirection.y = 0;
                    targetDirection = targetDirection.normalized;
                }
                
                transform.forward = Vector3.RotateTowards(transform.forward, targetDirection, rotSpeed * Time.deltaTime, 0.0f);
                
                // 如果平射，确保forward的y分量为0（限制在xz平面）
                if (isFlatShoot)
                {
                    Vector3 flatForward = transform.forward;
                    flatForward.y = 0;
                    if (flatForward.sqrMagnitude > 0.001f)
                    {
                        flatForward = flatForward.normalized;
                        transform.forward = flatForward;
                    }
                }
            }
            else
            {
                // 即使不追踪目标，如果平射也要确保forward的y分量为0
                if (isFlatShoot)
                {
                    Vector3 flatForward = transform.forward;
                    flatForward.y = 0;
                    if (flatForward.sqrMagnitude > 0.001f)
                    {
                        flatForward = flatForward.normalized;
                        transform.forward = flatForward;
                    }
                }
            }
            
            // 移动（使用Space.Self，沿着物体的forward方向移动）
            Vector3 forward = Vector3.forward;
            transform.Translate(forward * Speed * Time.deltaTime, Space.Self);
            
            // 如果平射，强制保持y坐标不变（确保y轴速度为0）
            if (isFlatShoot)
            {
                Vector3 pos = transform.position;
                transform.position = new Vector3(pos.x, initialYPosition, pos.z);
            }
        }
        private void OnTriggerEnter(Collider other)
        {

            if (OnHitEffect != null)
            {
                var onHitObj = Instantiate(OnHitEffect, transform.position, Quaternion.identity);
                var onHit = onHitObj.gameObject.AddComponent<AudioTrigger>();
                if (onHitClip != null)
                {
                    onHit.onClip = onHitClip;
                }
                
            }
            Destroy(gameObject);
        }

    }
}
