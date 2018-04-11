using System.IO;
using Modding;
using UnityEngine;
using ModCommon;

namespace CharmingMod
{
    using Components;
    /* 
     * For a nicer building experience, change 
     * SET MOD_DEST="K:\Games\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods"
     * in install_build.bat to point to your hollow knight mods folder...
     * 
     */
    public partial class CharmingMod : Mod<SaveSettings, ModSettings>, ITogglableMod
    {  
        public static CharmingMod Instance { get; private set; }

        CommunicationNode comms;

        public override void Initialize()
        {
            if(Instance != null)
            {
                Log("Warning: "+this.GetType().Name+" is a singleton. Trying to create more than one may cause issues!");
                return;
            }

            Instance = this;
            comms = new CommunicationNode();
            comms.EnableNode( this );

            Log( this.GetType().Name +" initializing!");

            SetupDefaulSettings();

            UnRegisterCallbacks();
            RegisterCallbacks();
        }

        void SetupDefaulSettings()
        {
            string globalSettingsFilename = Application.persistentDataPath + ModHooks.PathSeperator + GetType().Name + ".GlobalSettings.json";

            bool forceReloadGlobalSettings = false;
            if( GlobalSettings != null && GlobalSettings.SettingsVersion != CharmingSettingsVars.GlobalSettingsVersion )
            {
                forceReloadGlobalSettings = true;
            }
            else
            {
                Log( "Global settings version match!" );
            }

            if( forceReloadGlobalSettings || !File.Exists( globalSettingsFilename ) )
            {
                if( forceReloadGlobalSettings )
                {
                    Log( "Global settings are outdated! Reloading global settings" );
                }
                else
                {
                    Log( "Global settings file not found, generating new one... File was not found at: " + globalSettingsFilename );
                }

                GlobalSettings.Reset();

                GlobalSettings.SettingsVersion = CharmingSettingsVars.GlobalSettingsVersion;
            }

            SaveGlobalSettings();
        }

        ///Revert all changes the mod has made
        public void Unload()
        {
            UnRegisterCallbacks();
            comms.DisableNode();
            Instance = null;
        }

        //TODO: update when version checker is fixed in new modding API version
        public override string GetVersion()
        {
            return CharmingSettingsVars.ModVersion;
        }

        //TODO: update when version checker is fixed in new modding API version
        public override bool IsCurrent()
        {
            return true;
        }

        void RegisterCallbacks()
        {
        }

        void UnRegisterCallbacks()
        {
        }

        static PhysicsMaterial2D hbMat;
        static void DebugPrintObjectOnHit( Collider2D otherCollider, GameObject gameObject )
        {
            if( HeroController.instance.playerData.equippedCharm_15 )
            {
                Rigidbody2D body = otherCollider.GetComponentInParent<Rigidbody2D>();
                if( body != null )
                {
                    Vector2 blowDirection = otherCollider.transform.position - HeroController.instance.transform.position;
                    float blowPower = 40f;

                    TakeDamageFromImpact dmgOnImpact = body.gameObject.AddComponent<TakeDamageFromImpact>();
                    dmgOnImpact.blowVelocity = blowDirection.normalized * blowPower;
                    dmgOnImpact.oldMat = body.sharedMaterial;

                    if( hbMat == null )
                    {
                        hbMat = new PhysicsMaterial2D( "hb" );
                        hbMat.bounciness = .6f;
                        hbMat.friction = .2f;
                    }

                    body.sharedMaterial = hbMat;
                    body.velocity += blowDirection.normalized * blowPower;
                    body.isKinematic = false;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    body.angularVelocity = 20f;
                    body.gameObject.AddComponent<PreventOutOfBounds>();
                    DamageEnemies dme = body.gameObject.AddComponent<DamageEnemies>();
                    dme.damageDealt = (int)blowPower;
                }
            }
        }
    }
}
