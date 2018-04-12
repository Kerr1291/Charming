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

            int mask = 8;
            gameObject.layer |= mask;

            poob.onBoundCollision -= OnBoundsCollision;
            poob.onBoundCollision += OnBoundsCollision;
        }

        private void OnDisable()
        {
            gameObject.layer = oldLayer;
            //Dev.Log( " with layer (" + gameObject.layer + ")" );

            poob.onBoundCollision -= OnBoundsCollision;
        }

        private void OnDestroy()
        {
            gameObject.layer = oldLayer;
            //Dev.Log( " with layer (" + gameObject.layer + ")" );

            poob.onBoundCollision -= OnBoundsCollision;
        }

        void FixedUpdate()
        {
            body.position += blowVelocity * Time.fixedDeltaTime;
            transform.rotation = transform.rotation * Quaternion.Euler( new Vector3(0f,0f, blowVelocity.magnitude * Time.fixedDeltaTime));

            blowVelocity = blowVelocity * .955f;

            //in the case where we're just falling now
            if( blowVelocity.magnitude <= 10f * body.gravityScale )
            {
                transform.rotation = Quaternion.identity;
                gameObject.layer = oldLayer;
                //Dev.Log( " with layer (" + gameObject.layer + ")" );
                if( poob != null )
                    Destroy( poob );
                Destroy( this );
            }
            //TODO: kill an enemy that flies out of the scene
        }

        private void LateUpdate()
        {
            //Dev.Log(""+ blowVelocity.magnitude );
            //Dev.Log( ""+((blowVelocity.magnitude <= (10f * body.gravityScale)) ? "true" : "false") );
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
                DamageDealt = (int)(blowVelocity.magnitude * .25f),
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

            if( other.layer == 11 )
            {
                Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();
                if( rb != null )
                {
                    bool isEnemy = rb.gameObject.IsGameEnemy();
                    if( !isEnemy )
                        return;

                    blowVelocity = normal * blowVelocity.magnitude;
                    rb.gameObject.GetOrAddComponent<TakeDamageFromImpact>().blowVelocity = blowVelocity;
                    rb.gameObject.GetOrAddComponent<PreventOutOfBounds>();
                    DamageEnemies dme = rb.gameObject.GetOrAddComponent<DamageEnemies>();
                    dme.damageDealt = (int)blowVelocity.magnitude;
                    healthManager.Hit( hit );
                }
            }
        }
    }
}
