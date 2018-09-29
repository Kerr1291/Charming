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
    using System.Collections;
    using UnityEngine.SceneManagement;

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

            //Must manually set these for them to show up in the config file
            //I know this looks super wonky but it's the current best way to handle this
            GlobalSettings.GatheringSwarmChanges = GlobalSettings.GatheringSwarmChanges;
            GlobalSettings.WaywardCompassChanges = GlobalSettings.WaywardCompassChanges;
            GlobalSettings.HeavyBlowChanges = GlobalSettings.HeavyBlowChanges;

            Log( this.GetType().Name +" initializing!");

            SetupDefaulSettings();

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
            if (GlobalSettings.GatheringSwarmChanges)
            {
                On.GeoRock.OnEnable -= RegisterGeoRock;
                On.GeoRock.OnEnable += RegisterGeoRock;

                On.GeoRock.OnDisable -= UnRegisterGeoRock;
                On.GeoRock.OnDisable += UnRegisterGeoRock;

                On.GeoControl.Disable -= UnRegisterGeo;
                On.GeoControl.Disable += UnRegisterGeo;

                On.GeoControl.FixedUpdate -= ProcessGeoUpdate;
                On.GeoControl.FixedUpdate += ProcessGeoUpdate;
            }

            // Wayward Compass hooks
            if (GlobalSettings.WaywardCompassChanges)
            {
                ModHooks.Instance.CharmUpdateHook += CharmMapUpdate;
                ModHooks.Instance.HeroUpdateHook += HeroMapUpdate;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += UpdateMinimap;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += UpdateMinimap;
            }

            //Heavy Blow hooks
            if (GlobalSettings.HeavyBlowChanges)
            {
                ModHooks.Instance.SlashHitHook -= DebugPrintObjectOnHit;
                ModHooks.Instance.SlashHitHook += DebugPrintObjectOnHit;
            }
        }

        void UnRegisterCallbacks()
        {
            Dev.Where();

            //Gathering Swarm hooks
            On.GeoRock.OnEnable -= RegisterGeoRock;
            On.GeoRock.OnDisable -= UnRegisterGeoRock;
            On.GeoControl.Disable -= UnRegisterGeo;
            On.GeoControl.FixedUpdate -= ProcessGeoUpdate;

            // Wayward Compass / Minimap hooks
            if (minimap != null)
            {
                minimap.Unload();
                minimap = null;
            }
            
            ModHooks.Instance.HeroUpdateHook -= HeroMapUpdate;
            ModHooks.Instance.CharmUpdateHook -= CharmMapUpdate;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= UpdateMinimap;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= UpdateMinimap;

            //Heavy Blow hooks
            ModHooks.Instance.SlashHitHook -= DebugPrintObjectOnHit;
        }


        //static bool once = true;
        static string debugRecentHit = "";
        //static PhysicsMaterial2D hbMat;
        static void DebugPrintObjectOnHit( Collider2D otherCollider, GameObject gameObject )
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

        private static MethodInfo UpdateHitsOnRock = typeof(GeoRock).GetMethod("UpdateHitsLeftFromFSM", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo geoAttracted = typeof(GeoControl).GetField("attracted", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo geoBody = typeof(GeoControl).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance);

        private Dictionary<GeoRock, PlayMakerFSM> rocks = new Dictionary<GeoRock, PlayMakerFSM>();
        private Dictionary<GeoRock, GeoControl> rockChasers = new Dictionary<GeoRock, GeoControl>();
        private Dictionary<GeoControl, GeoRock> rockChasersReverseLookup = new Dictionary<GeoControl, GeoRock>();

        private void UnRegisterGeo(On.GeoControl.orig_Disable orig, GeoControl self, float waitTime)
        {
            orig(self, waitTime);

            //If we don't set this explicitly, it will get added back into the tracked geo during FixedUpdate
            geoAttracted.SetValue(self, false);

            if (rockChasersReverseLookup.ContainsKey(self))
            {
                rockChasers.Remove(rockChasersReverseLookup[self]);
                rockChasersReverseLookup.Remove(self);
            }
        }

        private void RegisterGeoRock(On.GeoRock.orig_OnEnable orig, GeoRock self)
        {
            orig(self);
            rocks.Add(self, self.gameObject.GetComponent<PlayMakerFSM>());
        }

        private void UnRegisterGeoRock(On.GeoRock.orig_OnDisable orig, GeoRock self)
        {
            orig(self);
            if (rocks.ContainsKey(self)) rocks.Remove(self);
            if (rockChasers.ContainsKey(self))
            {
                rockChasersReverseLookup.Remove(rockChasers[self]);
                rockChasers.Remove(self);
            }
        }

        private void ProcessGeoUpdate(On.GeoControl.orig_FixedUpdate orig, GeoControl self)
        {
            if ((bool)geoAttracted.GetValue(self))
            {
                //Remove rocks with no hits left
                foreach (GeoRock rock in rocks.Keys.ToList())
                {
                    UpdateHitsOnRock.Invoke(rock, null);
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
                if (rockChasersReverseLookup.ContainsKey(self)) closest = rockChasersReverseLookup[self];
                else closest = GetClosestRock(self);

                //orig(self) makes it go after the hero if there's no rocks to chase
                if (closest == null) orig(self);
                else
                {
                    //Cache this rock-geo pair if it isn't already
                    if (!rockChasers.ContainsKey(closest))
                    {
                        rockChasers.Add(closest, self);
                        rockChasersReverseLookup.Add(self, closest);
                    }

                    Rigidbody2D body = (Rigidbody2D)geoBody.GetValue(self);

                    //Copy pasted from GeoControl.FixedUpdate with hero changed to rock
                    Vector2 vector = new Vector2(closest.transform.position.x - self.transform.position.x, closest.transform.position.y - 0.5f - self.transform.position.y);
                    vector = Vector2.ClampMagnitude(vector, 1f);
                    vector = new Vector2(vector.x * 150f, vector.y * 150f);
                    body.AddForce(vector);
                    Vector2 vector2 = body.velocity;
                    vector2 = Vector2.ClampMagnitude(vector2, 20f);
                    body.velocity = vector2;

                    //Can't get collision/trigger enter events working
                    PlayMakerFSM rockFSM = rocks[closest];
                    if (DistBetween(self.transform.position, closest.transform.position) <= 1f && (rockFSM.ActiveStateName == "Idle" || rockFSM.ActiveStateName == "Gleam"))
                    {
                        rockFSM.SetState("Hit");
                    }
                }
            }
        }

        private GeoRock GetClosestRock(GeoControl geo)
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

        private float DistBetween(Vector2 first, Vector2 second)
        {
            return Vector2.Distance(first, second);
        }

        #endregion

        #region Wayward_Compass

        public Minimap minimap;

        private void UpdateMinimap(Scene from, Scene to)
        {
            if (HeroController.instance == null)
            {
                minimap.Unload();
                minimap = null;
            }
            GameManager.instance.StartCoroutine(UpdateMap());
        }

        private void UpdateMinimap(Scene from, LoadSceneMode lsm)
        {
            GameManager.instance.StartCoroutine(UpdateMap());
        }

        private IEnumerator UpdateMap()
        {
            if (minimap != null)
            {
                
                yield return new WaitForSeconds(0.2f);
            
                UpdateMinimap();
            }
            yield break;
        }

        private void UpdateMinimap()
        {
            if (HeroController.instance == null)
            {
                if (minimap != null)
                {
                    minimap.Unload();
                    minimap = null;
                }
            }

            if (minimap != null)
            {
                if (HeroController.instance.playerData.equippedCharm_2 && !GameManager.instance.IsCinematicScene() && GameManager.instance.IsGameplayScene() && !GameManager.instance.IsStagTravelScene())
                {
                    minimap.Show();
                }
                else
                {
                    minimap.Hide();
                }
                minimap.UpdateAreas();
            }
        }

        public void CharmMapUpdate(PlayerData pd, HeroController hc)
        {
            if (minimap != null)
            {
                if (pd.equippedCharm_2)
                {
                    GameMap map = GameManager.instance.gameMap.GetComponent<GameMap>();
                    map.SetupMap();
                    minimap.Show();

                    minimap.UpdateAreas();
                }
                else
                {
                    minimap.Hide();
                }
            }
        }

        public void HeroMapUpdate()
        {
            bool equippedCompass = PlayerData.instance.equippedCharm_2;
            if (equippedCompass)
            {
                GameManager.instance.UpdateGameMap();
                if (GameManager.instance.gameMap != null)
                {
                    if (minimap == null)
                    {
                        GameMap map = GameManager.instance.gameMap.GetComponent<GameMap>();
                        map.SetupMap();
                        minimap = new Minimap(map);
                        minimap.UpdateMap();
                        minimap.UpdateAreas();
                    }
                    else
                    {
                        minimap.UpdateMap();
                    }
                }
            }
        }

        #endregion
    }
}
