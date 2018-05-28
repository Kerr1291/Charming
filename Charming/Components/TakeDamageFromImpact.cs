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
        public float dampingRate = .955f;

        ///Callback used to determine if this component is "done" and should be removed
        public Func<TakeDamageFromImpact, bool> isFinished;

        protected virtual bool DefaultIsFinished( TakeDamageFromImpact self )
        {
            //return false;
            return blowVelocity.magnitude <= 10f * Body.gravityScale;
        }

        HealthManager healthManager;
        public HealthManager HealthManager { get {
                if( healthManager == null )
                    healthManager = GetComponent<HealthManager>();
                return healthManager;
            }
        }

        Rigidbody2D body;
        public Rigidbody2D Body {
            get {
                if( body == null )
                    body = GetComponent<Rigidbody2D>();
                return body;
            }
        }

        protected bool reflectedThisFrame;

        HitInstance ImpactHit {
            get {
                return new HitInstance()
                {
                    AttackType = AttackTypes.Splatter,
                    CircleDirection = false,
                    DamageDealt = 0,
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
            }
        }

        void OnEnable()
        {
            if( isFinished == null )
                isFinished = DefaultIsFinished;

            healthManager = GetComponent<HealthManager>();
            body = GetComponent<Rigidbody2D>();
            poob = GetComponent<PreventOutOfBounds>();

            if( poob != null )
            {
                poob.otherLayer = (1 << 8);

                poob.onBoundCollision -= OnBoundsCollision;
                poob.onBoundCollision += OnBoundsCollision;

                poob.onOtherCollision -= OnEnemyCollision;
                poob.onOtherCollision += OnEnemyCollision;
            }
        }

        private void OnDisable()
        {
            if( poob != null )
            {
                poob.onBoundCollision -= OnBoundsCollision;
                poob.onOtherCollision -= OnEnemyCollision;
            }

        }

        private void OnDestroy()
        {
            if( poob == null )
                return;
            poob.onBoundCollision -= OnBoundsCollision;
            poob.onOtherCollision -= OnEnemyCollision;
        }

        void FixedUpdate()
        {
            if( isFinished == null )
                isFinished = DefaultIsFinished;

            Body.position += blowVelocity * Time.fixedDeltaTime;
            transform.rotation = transform.rotation * Quaternion.Euler( new Vector3(0f,0f, blowVelocity.magnitude * Time.fixedDeltaTime));

            blowVelocity = blowVelocity * dampingRate;
            
            if( DefaultIsFinished(this) )
            {
                transform.rotation = Quaternion.identity;
                body.rotation = 0f;
                Destroy( this );
            }
            //TODO: kill an enemy that flies out of the scene
        }

        private void LateUpdate()
        {
            if( isFinished == null )
                isFinished = DefaultIsFinished;

            //Dev.Log(""+ blowVelocity.magnitude );
            //Dev.Log( ""+((blowVelocity.magnitude <= (10f * body.gravityScale)) ? "true" : "false") );
            reflectedThisFrame = false;  
        }

        void OnBoundsCollision(RaycastHit2D ray, GameObject self, GameObject other)
        {
            OnCollision( other, ray.normal );
        }

        void OnEnemyCollision( RaycastHit2D ray, GameObject self, GameObject other )
        {
            OnEnemyCollision( other, ray.normal );
        }

        void OnCollisionEnter2D( Collision2D collision )
        {
            OnCollision( collision.gameObject, collision.contacts[ 0 ].normal );
        }

        void OnCollision( GameObject other, Vector3 normal )
        {
            HitInstance hit = ImpactHit;
            //hit.DamageDealt = (int)( blowVelocity.magnitude * .25f );
            hit.DamageDealt = 0;

            //1000
            if( (other.layer & (1 << 8)) > 0 )
            { 
                HealthManager.Hit( hit );
                if(!reflectedThisFrame )
                    blowVelocity = normal * blowVelocity.magnitude;
                reflectedThisFrame = true;
            }
        }

        void OnEnemyCollision( GameObject other, Vector3 normal )
        {
            Dev.Where();
            HitInstance hit = ImpactHit;
            hit.DamageDealt = (int)( blowVelocity.magnitude * .25f );

            //1011
            //if( ( other.layer & 11 ) > 0 )
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
                    HealthManager.Hit( hit );
                }
            }
        }
    }
}
