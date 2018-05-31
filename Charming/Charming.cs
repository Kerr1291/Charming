using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public partial class CharmingMod : Mod<CharmingModSaveSettings, CharmingModSettings>, ITogglableMod
    {  
        public static CharmingMod Instance { get; private set; }

        CommunicationNode comms;

        HarmonyInstance modHooks;

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

            modHooks = ModCommon.HarmonyInstance.Create( "com.mods.hollowknight.charming" );

            UnRegisterCallbacks();
            RegisterCallbacks();

            Log( this.GetType().Name + " is done initializing!" );
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
            Dev.Log( "Mod done setting initializing!" );
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
            Dev.Where();

            //Gathering Swarm hooks
            PatchGeoRock();
            PatchGeoControl();

            //Wayward Compass hooks
            ModHooks.Instance.HeroUpdateHook -= RenderMinimap;
            ModHooks.Instance.HeroUpdateHook += RenderMinimap;

            //Heavy Blow hooks
            ModHooks.Instance.SlashHitHook -= ApplyOnHitEffects; 
            ModHooks.Instance.SlashHitHook += ApplyOnHitEffects;
        }

        void UnRegisterCallbacks()
        {
            Dev.Where();

            //Gathering Swarm hooks

            //Wayward Compass hooks
            ModHooks.Instance.HeroUpdateHook -= RenderMinimap;

            //Heavy Blow hooks
            ModHooks.Instance.SlashHitHook -= ApplyOnHitEffects;
        }


        //static bool once = true;
        static string debugRecentHit = "";
        //static PhysicsMaterial2D hbMat;
        static void ApplyOnHitEffects( Collider2D otherCollider, GameObject gameObject )
        {
            //if(once)
            //{
            //    once = false;
            //    HeroController.instance.superDash.WriteComponentTree( HeroController.instance.gameObject.name +"_Superdash" );
            //}

            //Dev.Where();
            if( otherCollider.gameObject.name != debugRecentHit )
            {
                //Dev.Log( "Hero at " + HeroController.instance.transform.position + " HIT: " + otherCollider.gameObject.name + " at (" + otherCollider.gameObject.transform.position + ")" + " with layer (" + otherCollider.gameObject.layer + ")" );
                debugRecentHit = otherCollider.gameObject.name;
            }

            //TODO: something in here throws a nullref
            
            if( !HeroController.instance.playerData.equippedCharm_15 )
                return;

            Rigidbody2D body = otherCollider.GetComponentInParent<Rigidbody2D>();

            if( body == null )
                return;

            bool isEnemy = body.gameObject.IsGameEnemy();
            if( !isEnemy )
                return;

            TakeDamageFromImpact dmgOnImpact = body.gameObject.GetOrAddComponent<TakeDamageFromImpact>();
            PreventOutOfBounds poob = body.gameObject.GetOrAddComponent<PreventOutOfBounds>();
            DamageEnemies dmgEnemies = body.gameObject.GetOrAddComponent<DamageEnemies>();

            Vector2 blowDirection = otherCollider.transform.position - HeroController.instance.transform.position;
            //blowDirection.y = 0f;
            float blowPower = 40f;

            dmgOnImpact.blowVelocity = blowDirection.normalized * blowPower;            
            dmgEnemies.damageDealt = (int)blowPower;
        }

        #region Gathering_Swarm

        private static MethodInfo GeoRock_UpdateHitsOnRock = typeof(GeoRock).GetMethod("UpdateHitsLeftFromFSM", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo GeoRock_OnEnable = typeof(GeoRock).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo GeoRock_OnDisable = typeof(GeoRock).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo registerGeoRock = typeof(CharmingMod).GetMethod("RegisterGeoRock", BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo unRegisterGeoRock = typeof(CharmingMod).GetMethod("UnRegisterGeoRock", BindingFlags.NonPublic | BindingFlags.Static);

        private static MethodInfo unRegisterGeo = typeof(CharmingMod).GetMethod("UnRegisterGeo", BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo processGeoUpdate = typeof(CharmingMod).GetMethod("ProcessGeoUpdate", BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo processGeoUpdatePost = typeof(CharmingMod).GetMethod("ProcessGeoUpdatePost", BindingFlags.NonPublic | BindingFlags.Static);


        private static MethodInfo GeoControl_FixedUpdate = typeof(GeoControl).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo GeoControl_Disable = typeof(GeoControl).GetMethod("Disable", BindingFlags.Public | BindingFlags.Instance);
        private static FieldInfo geoAttracted = typeof(GeoControl).GetField("attracted", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo geoBody = typeof(GeoControl).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Dictionary<GeoRock, PlayMakerFSM> rocks = new Dictionary<GeoRock, PlayMakerFSM>();
        private static Dictionary<GeoRock, GeoControl> rockChasers = new Dictionary<GeoRock, GeoControl>();
        private static Dictionary<GeoControl, GeoRock> rockChasersReverseLookup = new Dictionary<GeoControl, GeoRock>();
        
        void PatchGeoRock()
        {
            modHooks.Patch( GeoRock_OnEnable, null, new HarmonyMethod( registerGeoRock ) );
            modHooks.Patch( GeoRock_OnDisable, null, new HarmonyMethod( unRegisterGeoRock ) );
        }

        void PatchGeoControl()
        {
            modHooks.Patch( GeoControl_Disable, null, new HarmonyMethod( unRegisterGeo ) );
            modHooks.Patch( GeoControl_FixedUpdate, new HarmonyMethod( processGeoUpdate ), new HarmonyMethod( processGeoUpdatePost ) );
        }

        void UnPatchGeoRock()
        {
            modHooks.RemovePatch( GeoRock_OnEnable, HarmonyPatchType.All );
            modHooks.RemovePatch( GeoRock_OnDisable, HarmonyPatchType.All );
        }

        void UnPatchGeoControl()
        {
            modHooks.RemovePatch( GeoControl_Disable, HarmonyPatchType.All );
            modHooks.RemovePatch( GeoControl_FixedUpdate, HarmonyPatchType.All );
        }

        //special harmony param, get the instance associated with this hook
        private static void RegisterGeoRock( GeoRock __instance )
        {
            rocks.Add( __instance, __instance.gameObject.GetComponent<PlayMakerFSM>());
        }

        private static void UnRegisterGeoRock( GeoRock __instance )
        {
            if (rocks.ContainsKey( __instance ) ) rocks.Remove( __instance );
            if (rockChasers.ContainsKey( __instance ) )
            {
                rockChasersReverseLookup.Remove(rockChasers[ __instance ] );
                rockChasers.Remove( __instance );
            }
        }

        private static void UnRegisterGeo( GeoControl __instance, float waitTime )
        {
            //If we don't set this explicitly, it will get added back into the tracked geo during FixedUpdate
            geoAttracted.SetValue( __instance, false );

            if( rockChasersReverseLookup.ContainsKey( __instance ) )
            {
                rockChasers.Remove( rockChasersReverseLookup[ __instance ] );
                rockChasersReverseLookup.Remove( __instance );
            }
        }

        private static void ProcessGeoUpdate(GeoControl __instance, ref bool __state )
        {
            //save the attracted value in our local harmony state variable
            __state = (bool)geoAttracted.GetValue( __instance );

            if ( __state )
            {
                //Remove rocks with no hits left
                foreach (GeoRock rock in rocks.Keys.ToList())
                {
                    GeoRock_UpdateHitsOnRock.Invoke(rock, null);
                    if (rock.geoRockData.hitsLeft <= 0)
                    {
                        rocks.Remove(rock);
                        if (rockChasers.ContainsKey(rock))
                        {
                            rockChasersReverseLookup.Remove(rockChasers[rock]);
                            rockChasers.Remove(rock);
                        }
                    }
                }

                //If this geo is already after a rock, use that. Otherwise get the closest one
                GeoRock closest;
                if (rockChasersReverseLookup.ContainsKey(__instance )) closest = rockChasersReverseLookup[__instance ];
                else closest = GetClosestRock(__instance );

                //orig(self) makes it go after the hero if there's no rocks to chase
                if (closest != null)
                {
                    //Cache this rock-geo pair if it isn't already
                    if (!rockChasers.ContainsKey(closest))
                    {
                        rockChasers.Add(closest, __instance );
                        rockChasersReverseLookup.Add(__instance , closest);
                    }

                    Rigidbody2D body = (Rigidbody2D)geoBody.GetValue(__instance );

                    //Copy pasted from GeoControl.FixedUpdate with hero changed to rock
                    Vector2 vector = new Vector2(closest.transform.position.x - __instance .transform.position.x, closest.transform.position.y - 0.5f - __instance .transform.position.y);
                    vector = Vector2.ClampMagnitude(vector, 1f);
                    vector = new Vector2(vector.x * 150f, vector.y * 150f);
                    body.AddForce(vector);
                    Vector2 vector2 = body.velocity;
                    vector2 = Vector2.ClampMagnitude(vector2, 20f);
                    body.velocity = vector2;

                    //Can't get collision/trigger enter events working
                    PlayMakerFSM rockFSM = rocks[closest];
                    if (DistBetween(__instance .transform.position, closest.transform.position) <= 1f && (rockFSM.ActiveStateName == "Idle" || rockFSM.ActiveStateName == "Gleam"))
                    {
                        rockFSM.SetState("Hit");
                    }

                    //disables the attracted state so the default logic does not run
                    geoAttracted.SetValue( __instance, false );
                }
            }
        }

        //on post process, restore the attracted value
        private static void ProcessGeoUpdatePost( GeoControl __instance, bool __state )
        {
            geoAttracted.SetValue( __instance, __state );
        }

        private static GeoRock GetClosestRock(GeoControl geo)
        {
            //This isn't efficient at all but it's fine because the value is cached in rockChasers
            List<GeoRock> validRocks = rocks.Where(rockPair => !rockChasers.ContainsKey(rockPair.Key)).Select(rockPair => rockPair.Key).ToList();
            if (validRocks.Count == 0) return null;

            GeoRock closest = validRocks[0];
            float closestDist = DistBetween(validRocks[0].transform.position, geo.transform.position);

            foreach (GeoRock rock in validRocks)
            {
                float newDist = DistBetween(rock.transform.position, geo.transform.position);
                if (newDist < closestDist)
                {
                    closestDist = newDist;
                    closest = rock;
                }
            }

            return closest;
        }

        private static float DistBetween(Vector2 first, Vector2 second)
        {
            return Vector2.Distance( first, second );
                //(float)Math.Sqrt(Math.Pow(first.x - second.x, 2) + Math.Pow(first.y - second.y, 2));
        }

        #endregion

        #region Wayward_Compass

        private const int MAP_SCALE = 4;
        private const float MAP_UPDATE_RATE = 1 / 30f;

        private UnityEngine.UI.Image minimap;
        private RenderTexture renderTex = new RenderTexture(Screen.width / MAP_SCALE, Screen.height / MAP_SCALE, 0);
        private Texture2D tex = new Texture2D(Screen.width / MAP_SCALE, Screen.height / MAP_SCALE);
        private float lastMapTime = 0f;

        private void RenderMinimap()
        {
            //If charm isn't equipped, clean up the map objects and exit
            if (!PlayerData.instance.GetBool("equippedCharm_2"))
            {
                if (minimap != null)
                {
                    UnityEngine.Object.Destroy(minimap.gameObject.transform.parent.gameObject);
                }

                return;
            }

            //Create the map object if it doesn't exist yet
            if (minimap == null)
            {
                GameObject minimapCanvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
                CanvasUtil.CreateImagePanel(minimapCanvas, CanvasUtil.NullSprite(new byte[] { 0xFF, 0x00, 0x00, 0xFF }), new CanvasUtil.RectData(Vector2.zero, Vector2.zero, new Vector2(0.7875f, 0.745f), new Vector2(0.9925f, 0.955f))).GetComponent<UnityEngine.UI.Image>().preserveAspect = false;
                minimap = CanvasUtil.CreateImagePanel(minimapCanvas, CanvasUtil.NullSprite(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }), new CanvasUtil.RectData(Vector2.zero, Vector2.zero, new Vector2(0.79f, 0.75f), new Vector2(0.99f, 0.95f))).GetComponent<UnityEngine.UI.Image>();
            }

            //Aiming for 30 fps here because higher would kill fps
            if (Time.realtimeSinceStartup - lastMapTime > MAP_UPDATE_RATE)
            {
                CameraController cam = GameManager.instance.cameraCtrl;
                
                //Figure out the target scale and which axis to move on
                float camScaleX = 30f / (cam.xLimit + 14.6f);
                float camScaleY = ((9f / 16f) * 30f) / (cam.yLimit + 8.3f);
                bool movingX = camScaleX < camScaleY;
                float camScale = Math.Max(camScaleX, camScaleY);

                //Save old values to restore after rendering minimap
                float oldScale = GameCameras.instance.tk2dCam.ZoomFactor;
                bool vignette = HeroController.instance.vignette.enabled;
                Vector3 oldPos = cam.gameObject.transform.position;
                Vector3 oldTargetPos = cam.camTarget.transform.position;

                //Remove fade out away from player and zoom to target scale
                HeroController.instance.vignette.enabled = false;
                GameCameras.instance.tk2dCam.ZoomFactor = camScale;

                //Move along the x or y axis
                if (movingX)
                {
                    float camMin = 14.6f / camScale;
                    float camMax = (cam.xLimit + 14.6f) - 14.6f / camScale;

                    float camX = HeroController.instance.gameObject.transform.position.x;
                    if (camX < camMin) camX = camMin;
                    else if (camX > camMax) camX = camMax;

                    cam.SnapTo(camX, (cam.yLimit + 8.3f) / 2f);
                }
                else
                {
                    float camMin = 8.3f / camScale;
                    float camMax = (cam.yLimit + 8.3f) - 8.3f / camScale;

                    float camY = HeroController.instance.gameObject.transform.position.y;
                    if (camY < camMin) camY = camMin;
                    else if (camY > camMax) camY = camMax;

                    cam.SnapTo((cam.xLimit + 14.6f) / 2f, camY);
                }

                //Render the game onto the render texture
                cam.cam.targetTexture = renderTex;
                cam.cam.Render();
                cam.cam.targetTexture = null;

                //Restore old values
                HeroController.instance.vignette.enabled = vignette;
                GameCameras.instance.tk2dCam.ZoomFactor = oldScale;
                cam.gameObject.transform.position = oldPos;
                cam.camTarget.transform.position = oldTargetPos;

                //Apply the downscaled game image to a texture
                RenderTexture.active = renderTex;
                tex.ReadPixels(new Rect(0, 0, Screen.width / MAP_SCALE, Screen.height / MAP_SCALE), 0, 0);
                tex.Apply(false);
                RenderTexture.active = null;

                //I don't trust Unity to not leak without explicit destructors
                UnityEngine.Object.Destroy(minimap.sprite);

                //Create a sprite from the texture and stick it on the minimap
                minimap.sprite = Sprite.Create(tex, new Rect(0, 0, Screen.width / MAP_SCALE, Screen.height / MAP_SCALE), Vector2.zero);

                //Keep track of frame time
                lastMapTime = Time.realtimeSinceStartup;
            }
        }

        #endregion
    }
}
