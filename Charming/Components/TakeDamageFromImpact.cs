using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ModCommon;

namespace CharmingMod.Components
{
    public class TakeDamageFromImpact : MonoBehaviour
    {
        public PreventOutOfBounds poob;
        public Vector2 blowVelocity;
        HealthManager healthManager;
        Rigidbody2D body;
        int oldLayer;
        bool reflectedThisFrame;

        void OnEnable()
        {
            oldLayer = gameObject.layer;
            healthManager = GetComponent<HealthManager>();
            body = GetComponent<Rigidbody2D>();
            poob = GetComponent<PreventOutOfBounds>();

            poob.onBoundCollision -= OnBoundsCollision;
            poob.onBoundCollision += OnBoundsCollision;
        }

        private void OnDisable()
        {
            poob.onBoundCollision -= OnBoundsCollision;
        }

        void FixedUpdate()
        {
            body.position += blowVelocity * Time.fixedDeltaTime;
            body.rotation += 100f * .985f * Time.fixedDeltaTime;

            blowVelocity = blowVelocity * .985f;

            if( blowVelocity.magnitude <= 0.1f )
            {
                transform.rotation = Quaternion.identity;
                gameObject.layer = oldLayer;
                Destroy( this );
            }
        }

        private void LateUpdate()
        {
            reflectedThisFrame = false;
        }

        void OnBoundsCollision(RaycastHit2D ray, GameObject self, GameObject other)
        {
            OnCollision( other, ray.normal );
        }

        void OnCollisionEnter2D( Collision2D collision )
        {
            OnCollision( collision.gameObject, collision.contacts[ 0 ].normal );
        }

        void OnCollision( GameObject other, Vector3 normal )
        {
            HitInstance hit = new HitInstance()
            {
                AttackType = AttackTypes.Splatter,
                CircleDirection = false,
                DamageDealt = (int)(blowVelocity.magnitude),
                Direction = 0f,
                IgnoreInvulnerable = false,
                IsExtraDamage = false,
                MagnitudeMultiplier = 1f,
                MoveAngle = 0f,
                MoveDirection = false,
                Multiplier = 1f,
                Source = HeroController.instance.gameObject,
                SpecialType = SpecialTypes.None
            };

            if( other.layer == 8 )
            {
                healthManager.Hit( hit );
                if(!reflectedThisFrame )
                    blowVelocity = normal * blowVelocity.magnitude;
                reflectedThisFrame = true;
            }

            //if( other.layer == 11 )
            //{
            //    Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            //    if( rb != null )
            //    {
            //        blowVelocity = normal * blowVelocity.magnitude;
            //        other.GetOrAddComponent<TakeDamageFromImpact>().blowVelocity = blowVelocity;
            //        other.GetOrAddComponent<PreventOutOfBounds>();
            //        DamageEnemies dme = other.GetOrAddComponent<DamageEnemies>();
            //        dme.damageDealt = (int)blowVelocity.magnitude;
            //        healthManager.Hit( hit );
            //    }
            //}
        }
    }
}
