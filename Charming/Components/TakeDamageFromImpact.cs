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
        public Vector2 blowVelocity;
        HealthManager healthManager;
        Rigidbody2D body;
        void OnEnable()
        {
            healthManager = GetComponent<HealthManager>();
            body = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            body.position += blowVelocity * Time.fixedDeltaTime;
            body.rotation += 100f * .955f * Time.fixedDeltaTime;
            blowVelocity = blowVelocity * .995f;
            DamageEnemies dme = gameObject.AddComponent<DamageEnemies>();
            dme.damageDealt = (int)body.velocity.magnitude;
            if( blowVelocity.magnitude <= 0.1f )
                Destroy( this );
        }

        void OnCollisionEnter2D( Collision2D collision )
        {
            HitInstance hit = new HitInstance()
            {
                AttackType = AttackTypes.Splatter,
                CircleDirection = false,
                DamageDealt = (int)(blowVelocity.magnitude * 2f),
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

            if( collision.gameObject.layer == 8 )
            {
                healthManager.Hit( hit );
                blowVelocity = collision.contacts[ 0 ].normal * blowVelocity.magnitude;
            }

            if( collision.gameObject.layer == 11 )
            {
                gameObject.layer = 13;
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if( rb != null )
                {
                    blowVelocity = collision.contacts[ 0 ].normal * blowVelocity.magnitude;
                    collision.gameObject.AddComponent<TakeDamageFromImpact>().blowVelocity = blowVelocity * 2f;
                    collision.gameObject.AddComponent<PreventOutOfBounds>();
                    DamageEnemies dme = collision.gameObject.AddComponent<DamageEnemies>();
                    dme.damageDealt = (int)blowVelocity.magnitude;
                    //rb.velocity += body.velocity;
                    healthManager.Hit( hit );
                }
            }
        }
    }
}
