using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Harmony;
using UnityEngine.Networking;
using UnityEngine.UI;
using ModLoader;
using System.Linq.Expressions;
using VTOLVR.Multiplayer;
using Microsoft.SqlServer.Server;

namespace Jester
{
    public class JesterAI : VTOLMOD
    {
        
        [Header("Lists for AI")]
        private List<Actor> knownHostiles = new List<Actor>();




        [Header("Radio Pause")]
        public AudioClip[] radioPause;


        [Header("Wire Catches")]

        public AudioClip[] bolterLanding;
        public AudioClip[] oneWire;
        public AudioClip[] twoWire;
        public AudioClip[] threeWire;
        public AudioClip[] fourWire;
        public AudioClip[] waveOff;



        [Header("Crashes")]
        public AudioClip[] seaCrash;
        public AudioClip[] groundCrash;

        [Header("Ejection")]
        public AudioClip[] ejectionLines;

        [Header("Damage")]
        public AudioClip[] engineDamage;
        public AudioClip[] wingDamage;
        public AudioClip[] bodyDamage;

        [Header("Position Callouts")]
        public AudioClip[] one_oclock;
        public AudioClip[] two_oclock;
        public AudioClip[] three_oclock;
        public AudioClip[] four_oclock;
        public AudioClip[] five_oclock;
        public AudioClip[] six_oclock;
        public AudioClip[] seven_oclock;
        public AudioClip[] eight_oclock;
        public AudioClip[] nine_oclock;
        public AudioClip[] ten_oclock;
        public AudioClip[] eleven_oclock;
        public AudioClip[] twelve_oclock;

        [Header("Braa Callouts")]
        public AudioClip[] BRAA;
        public AudioClip[] Bearing;
        public AudioClip[] Range;
        public AudioClip[] Altitude;
        public AudioClip[] speedCallout;
        public AudioClip[] Attitude;

        public AudioClip[] groupBullseyeClips;
        public AudioClip[] groupBraaClips;
        public AudioClip[] hostileBullseyeClips;
        public AudioClip[] hostileBraaClips;

        [Header("Callouts for Enemies")]
        public AudioClip[] EnemyCallout;


        [Header("Numbers")]
        public AudioClip[] number;
        [Header("Composite Numbers")]
        public AudioClip[] compNumber;

        [Header("Range Numbers")]
        public AudioClip[] rangeClips;
        public AudioClip[] rangeType;



        [Header("Altitude")]
        public AudioClip[] alt_Hundred;
        public AudioClip[] alt_Thousand;

        [Header("Cardinals")]
        public AudioClip[] cardinalClips;
        [Header("Azimuth")]
        public AudioClip[] hotClips;
        public AudioClip[] coldClips;
        public AudioClip[] fastClips;


        [Header("BFM")]
        public AudioClip[] bfm_Six;
        public AudioClip[] bfm_Ahead;
        public AudioClip[] bfm_Coming;


        [Header("Picture")]
        public AudioClip[] pictureCall;


        [Header("Pop Up")]
        public AudioClip[] popUpCall;

        [Header("Placement")]
        public AudioClip[] placementCall;


        [Header("Letters")]
        public AudioClip[] letter;
        

        [Header("Altitude")]
        public AudioClip[] lowClips;
        public AudioClip[] highClips;

        [Header("Phonetics")]
        public AudioClip[] phonetics;
        [Header("Composite Phonetics")]
        public AudioClip[] compPhonetics;

        [Header("Clean")]
        public AudioClip[] cleanCalls;

        [Header("Fuel")]
        public AudioClip[] fuelCalls;


        [Header("Grand Slam")]
        public AudioClip[] slamCalls;


        [Header("Close In")]
        public AudioClip[] closeInFightCalls;
        public AudioClip[] mergedClips;

        public static GameObject jesteraiobj;
        public JesterAI jesteraicomponent;
        public const string HarmonyId = "Bovine.Jester";
        public static CommRadioManager CommRadioManager2;
        public static GameObject go;
        public static List<ContactGroup> popupGroups = new List<ContactGroup>();
        public bool collectingPopups = false;
        public List<AudioClip> audioString = new List<AudioClip>();
        private float altfloat;
        public AudioClip[] currentAltitude;
        public AudioClip audioClip;
        private float altnum;
        private List<Actor> knownHostilesList;
        private List<float> range = new List<float>();
        private float sqrDistToPlayer;
        private Actor lowestRangeActor;
        private float lowestRange;

        public class ContactGroup
        {
            
            public int count;

            public FixedPoint globalPos;

            public Vector3 velocity;

            public float sqrDistToPlayer;
        }

        public override void ModLoaded()
        {

           HarmonyInstance harmonyInstance = HarmonyInstance.Create(HarmonyId);

            harmonyInstance.PatchAll();
            VTOLAPI.MissionReloaded = (UnityAction)Delegate.Combine(VTOLAPI.MissionReloaded, new UnityAction(this.MissionReloaded));
            VTOLAPI.SceneLoaded = (UnityAction<VTOLScenes>)Delegate.Combine(VTOLAPI.SceneLoaded, new UnityAction<VTOLScenes>(this.SceneLoaded));
            base.ModLoaded();
        }

        private void SceneLoaded(VTOLScenes scene)
        {
            switch (scene)
            {
                case VTOLScenes.ReadyRoom:
                    break;
                case VTOLScenes.Akutan:
                    Log("Akutan Map Loaded");
                    base.StartCoroutine("SetUpScene");
                    break;
                case VTOLScenes.CustomMapBase:
                    Log("Map Loaded");
                    base.StartCoroutine("SetUpScene");
                    break;
                case VTOLScenes.LoadingScene:
                    break;
            }
        }

        private void MissionReloaded()
        {
            base.StartCoroutine("SetUpScene");

        }

        private IEnumerator SetUpScene()
        {
            while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady || FlightSceneManager.instance.switchingScene)
            {
                yield return null;
            }

            go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("Jester 0.0");
            
            
            jesteraiobj = GetChildWithName(go, "JesterAI", true);
            
            InvokeRepeating("CheckPosition", 10f, 30f);
            InvokeRepeating("ReportNearestEnemyAir", 10f, 10f);

            yield break;
        }

        

        [HarmonyPatch(typeof(CommRadioManager), nameof(CommRadioManager.Awake), new Type[] { })]
        public static class CommRadioManagerFinder
        {
            

            public static void Postfix(CommRadioManager __instance)
            {
                //Debug.Log("CommRadioManagerFinder 1");
                CommRadioManager2 = __instance;
                __instance.SetCommsVolumeCopilot(10f);
            }
        }

        [HarmonyPatch(typeof(ATCVoiceProfile), nameof(ATCVoiceProfile.PlayLSOXwire), new Type[] { typeof(int)})]
        public static class PatchXWireCallout
        {
            private static JesterAI jestcomp;
            private static AudioClip wireclip;
            private static AudioClip pauseclip;
            public static void Postfix(ATCVoiceProfile __instance, ref int idx)
            {
                //Debug.Log("Jester ATC W 0.0");
                if (jesteraiobj == null) { return; }
                jestcomp = jesteraiobj.GetComponent<JesterAI>();
                pauseclip = RandomClip(jestcomp.radioPause);

                switch (idx)
                {
                    case 0:
                        wireclip = RandomClip(jestcomp.oneWire);
                        //Debug.Log("Jester ATC W 0.1");
                        break;

                    case 1:
                        wireclip = RandomClip(jestcomp.twoWire);
                        //Debug.Log("Jester ATC W 0.2");
                        break;
                    case 2:
                        wireclip = RandomClip(jestcomp.threeWire);
                        //Debug.Log("Jester ATC W 0.3");
                        break;
                    case 3:
                        wireclip = RandomClip(jestcomp.fourWire);
                        //Debug.Log("Jester ATC W 0.4");
                        break;
                    default:

                        break;
   
                }



                
                
                
                //Debug.Log("Jester ATC W 2.0");

                CommRadioManager2.PlayCopilotMessage(pauseclip, true);
                CommRadioManager2.PlayCopilotMessage(wireclip, true);
            }


        }

        [HarmonyPatch(typeof(ATCVoiceProfile), nameof(ATCVoiceProfile.PlayLSOBolter), new Type[] { })]
        public static class PatchBolterCallout
        {
            private static JesterAI jestcomp;
            private static AudioClip pauseclip;

            public static void Postfix(ATCVoiceProfile __instance)
            {
                if (jesteraiobj == null) { return; }
                //Debug.Log("Jester ATC PB 0.0");
                jestcomp = jesteraiobj.GetComponent<JesterAI>();
                pauseclip = RandomClip(jestcomp.radioPause);
                //Debug.Log("Jester ATC PB 1.0");

                AudioClip bolterac = RandomClip(jestcomp.bolterLanding);
                //Debug.Log("Jester ATC PB 2.0");
                CommRadioManager2.PlayCopilotMessage(pauseclip, true);
                CommRadioManager2.PlayCopilotMessage(bolterac, true);
            }


        }


        [HarmonyPatch(typeof(Radar), nameof(Radar.DetectActor), new Type[] {typeof(Actor) })]
        public static class PatchDetectActor
        {
            

            public static void Postfix(Radar __instance, ref Actor a)
            {
                if (!jesteraiobj)
                {
                    return;
                }
                if (__instance.enabled)
                {
                    jesteraiobj.GetComponent<JesterAI>().JesterDetectedActor(a, true);
                }
            }
        }



        public static AudioClip RandomClip(AudioClip[] clips)
        {
            //Debug.Log("Jester RC 0.0");
            

            if (clips != null && clips.Length != 0)
            {
                //Debug.Log("Jester RC 1.0");
                return clips[UnityEngine.Random.Range(0, clips.Length)];
            }
            else
            {
                //Debug.Log("Jester RC 2.0");
                
            }
            return null;
        }

        public virtual void CheckPosition()
        {
            jesteraicomponent = jesteraiobj.GetComponent<JesterAI>();
            //Debug.Log("CP 0.0 " );
            CalloutHeightSpeed();
        }
        
        public virtual void CalloutHeightSpeed()
        {
            float num = WaterPhysics.GetAltitude(go.GetComponent<Transform>().position);
            jesteraicomponent.audioString.Clear();
            //Debug.Log("CHS 0.0 " + num);
            //Altitude Callouts
            jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.Altitude)
                });
            //Debug.Log("CHS 2.0 ");
            num = jesteraicomponent.ConvertedAltitude(num);
            altnum = num;
            num /= 1000f;
            int num2 = Mathf.RoundToInt(num);
            //Debug.Log("CHS 3.0 " +num2);
            num2 = Mathf.Max(1, num2);
            AudioClip audioClip;
            if (num2 < 9)
            {
                //Debug.Log("CHS 4.0 ");
                audioClip = jesteraicomponent.compNumber[num2 * 2];
            }
            else if (num2 <= 20)
            {
                //Debug.Log("CHS 5.0 ");
                audioClip = jesteraicomponent.highClips[(num2 - 9) / 2];
            }
            else
            {
                //Debug.Log("CHS 6.0 ");
                int num3 = 5 + (num2 - 15) / 5;
                num3 = Mathf.Clamp(num3, 0, jesteraicomponent.highClips.Length - 1);
                audioClip = jesteraicomponent.highClips[num3];
                //Debug.Log("CHS 3.1.0 " + num3);
            }
            //Debug.Log("CHS 7.0 ");
            jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
            {
            audioClip,
            RandomClip(jesteraicomponent.alt_Thousand)
            });
            //Speed Callouts
            jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.speedCallout)
                });
            float speed = go.GetComponent<FlightInfo>().surfaceSpeed;
            //Debug.Log("Speed 1.0: " + speed);
            float speedconv = jesteraicomponent.ConvertedSpeed(speed);
            //Debug.Log("Speed 2.0: " + speedconv);
            int speedint = Mathf.RoundToInt(speedconv);
            //Debug.Log("Speed 3.0: " + speedint);
            int thousands = speedint / 1000 % 10;
            //Debug.Log("Speed 3.0: " + thousands);
            int hundreds = speedint / 100 % 10;
            //Debug.Log("Speed 3.0: " + hundreds);
            int tens = speedint / 10 % 10;
            //Debug.Log("Speed 3.0: " + tens);
            int ones = speedint % 10;
            //Debug.Log("Speed 3.0: " + ones);
            jesteraicomponent.AppendNumbers(jesteraicomponent.audioString, new int[]
            {
            thousands,
            hundreds,
            tens,
            ones
            });

            //End Callout Creation
            //Debug.Log("CHS 7.0 ");
            if (!CommRadioManager2)
            {
                //Debug.Log("8.0");
            }
            if (altnum < 200 || speed < 75 )
            { }
            else
            {
                CommRadioManager2.PlayMessageString(jesteraicomponent.audioString);
            }
        }
        


        public virtual void ReportNearestEnemyAir()
        {
            jesteraicomponent.audioString.Clear();
            Actor nearenemy = FindNearestEnemy();
            //Debug.Log("Nearest Enemy: " + nearenemy + ", range: " +jesteraicomponent.lowestRange);
            if(jesteraicomponent.lowestRange<3000f)
            {
                int enemyClockPosition = FindEnemyClockPosition(nearenemy);
                switch (enemyClockPosition)
                {
                    case 0:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.twelve_oclock)
                });
                        break;
                    case 1:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.one_oclock)
                });
                        break;
                    case 2:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.two_oclock)
                });
                        break;
                    case 3:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.three_oclock)
                });
                        break;
                    case 4:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.four_oclock)
                });
                        break;
                    case 5:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.five_oclock)
                }); break;

                    case 6:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.six_oclock)
                });
                        break;
                    case 7:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.seven_oclock)
                });
                        break;
                    case 8:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.eight_oclock)
                });
                        break;
                    case 9:

                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.nine_oclock)
                });
                                                break;
                    case 10:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.ten_oclock)
                });
                                                break;
                    case 11:
                        jesteraicomponent.AppendClips(jesteraicomponent.audioString, new AudioClip[]
                {
                RandomClip(jesteraicomponent.eleven_oclock)
                });
                        break;

                        default: break;
                }
            }
            CommRadioManager2.PlayMessageString(jesteraicomponent.audioString);
        }

        public virtual int FindEnemyClockPosition(Actor nearenemy)
        {
            int playerHeading = Mathf.RoundToInt(go.GetComponent<FlightInfo>().heading);
            //Debug.Log("pH = " + playerHeading);
            Vector3 fromPt = go.GetComponent<Transform>().position;
            //Debug.Log("frompt = " + fromPt);
            Vector3 toPt = nearenemy.GetComponent<Transform>().position;
            //Debug.Log("topt = " + toPt);
            int bearing = Mathf.RoundToInt(VectorUtils.Bearing(fromPt, toPt));
            //Debug.Log("bearing = " + bearing);
            int degrees = playerHeading- bearing;
            //Debug.Log("degrees = " + degrees);
            float clockpositionf = degrees / 30;
            //Debug.Log("clock f = " + clockpositionf);
            int clockpositioni = Mathf.RoundToInt(clockpositionf);
            //Debug.Log("clock i = " + clockpositioni);
            if(clockpositioni<0)
            {
                clockpositioni = clockpositioni * -1;
            }
            return clockpositioni;
            
        }

        public virtual Actor FindNearestEnemy()
        {
            
                
            knownHostilesList = jesteraiobj.GetComponent<JesterAI>().knownHostiles;
            jesteraicomponent = jesteraiobj.GetComponent<JesterAI>();
            int actorcount = 1;
            jesteraicomponent.lowestRange = 10000000f;
            foreach (Actor actor in knownHostilesList)
            {
                
                if (actor && actor.team != go.GetComponent <Actor>().team && actor.discovered && actor.detectedByAllied && actor.alive)
                {
                    //Debug.Log("fne 1.0" + actor + ", Count: " + actorcount);

                     float rangef = jesteraicomponent.FindRange(jesteraicomponent.getPlayerPos(), actor.GetComponent<Transform>().position);
                    //Debug.Log("fne 1.0.1" + rangef);
                    
                    //Debug.Log("fne 1.0.1");
                    //Debug.Log("fne 1.1" + rangef + "lowest = " + jesteraicomponent.lowestRange);
                    if (rangef < jesteraicomponent.lowestRange)
                    {
                        //Debug.Log("fne 1.3");
                        jesteraicomponent.lowestRangeActor = actor;
                        //Debug.Log("fne 1.4");
                        jesteraicomponent.lowestRange = rangef;
                    }
                    //Debug.Log("fne 1.5");
                    actorcount++;

                }

                
            }
            return jesteraicomponent.lowestRangeActor;
        }

        public virtual float FindRange(Vector3 playerPos, Vector3 enemyPos)
        {
            //Debug.Log("From = " + playerPos + "; " + enemyPos);
            float num = Vector3.ProjectOnPlane(playerPos - enemyPos, Vector3.up).magnitude;
            //Debug.Log("FR 1.0 Range = " + num);
            return num;

        }


        public virtual void JesterDetectedActor(Actor a, bool callPopups)
        {
            //Debug.Log("JDA 0.0: " + a.name + ", " + a.actorName + ", " + a.actorID);
            if (a.team == go.GetComponentInChildren<Actor>().team || a.role == Actor.Roles.Missile || !a.alive)
            {
                //Debug.Log("JDA 1.0");
                return;
            }
            foreach (Actor actor1 in this.knownHostiles)
            {
            //Debug.Log("JDA 1.1" + actor1.name + ", " + actor1.actorName + ", " + actor1.actorID);

            }

            //Debug.Log("JDA 2.0");
            if (!this.knownHostiles.Contains(a))
            {
                this.knownHostiles.Add(a);
                //Debug.Log("JDA 2.1 " +knownHostiles.Count() );
                if (callPopups && FlightSceneManager.instance.playerActor)
                {
                    bool flag = false;
                    int num = 0;
                    //Debug.Log("JDA 2.2 +" + popupGroups.Count);
                    while (num < popupGroups.Count && !flag)
                    {
                        //Debug.Log("JDA 2.3");
                        JesterAI.ContactGroup contactGroup = popupGroups[num];
                        //Debug.Log("JDA 2.3.1");
                        if ((a.position - contactGroup.globalPos.point).sqrMagnitude < 2250000f && Vector3.Dot(contactGroup.velocity.normalized, a.velocity.normalized) > 0.7f)
                        {
                            //Debug.Log("JDA 2.4");
                            int count = contactGroup.count;
                            contactGroup.count++;
                            contactGroup.globalPos = new FixedPoint((contactGroup.globalPos.point * (float)count + a.position) / (float)contactGroup.count);
                            //Debug.Log("JDA 2.5");
                            contactGroup.velocity = (contactGroup.velocity * (float)count + a.velocity) / (float)contactGroup.count;
                            contactGroup.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup.globalPos.point).sqrMagnitude;
                            flag = true;
                        }
                        num++;
                    }
                    //Debug.Log("JDA 2.6");
                    if (!flag)
                    {
                        //Debug.Log("JDA 2.7");
                        JesterAI.ContactGroup contactGroup2 = new JesterAI.ContactGroup();
                        contactGroup2.count = 1;
                        contactGroup2.globalPos = new FixedPoint(a.position);
                        //Debug.Log("JDA 2.8");
                        contactGroup2.velocity = a.velocity;
                        contactGroup2.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup2.globalPos.point).sqrMagnitude;
                        popupGroups.Add(contactGroup2);
                    }

                }
                if (!this.collectingPopups)
                {
                    this.StartCoroutine(PopupRoutine());
                }
            }
        }

        public virtual IEnumerator PopupRoutine()
        {
            //Debug.Log("PUR 1.0");
            this.collectingPopups = true;
            yield return new WaitForSeconds(6f);
            this.collectingPopups = false;
            
            if (go.GetComponent<Actor>().alive && this.IsPlayerTeam())
            {
                this.ReportPopups(popupGroups, 0, 3);
                
            }
            //Debug.Log("PUR 1.1");
            popupGroups.Clear();
            //Debug.Log("PUR 1.2");
            yield break;
        }

        public virtual bool IsPlayerTeam()
        {
            if (VTOLMPUtils.IsMultiplayer())
            {
                PlayerInfo localPlayerInfo = VTOLMPLobbyManager.localPlayerInfo;
                return localPlayerInfo != null && localPlayerInfo.chosenTeam && localPlayerInfo.team == go.GetComponent<Actor>().team;
            }
            return go.GetComponent<Actor>().team == Teams.Allied;
        }


        public virtual void ReportPopups(List<ContactGroup> groups, int offset, int count)
        {
            //Debug.Log("ReportPopups");
            audioString.Clear();
            //Debug.Log("RPU 1.1");
            this.AppendCallsigns(this.audioString);
            //Debug.Log("RPU 1.2");
            this.AppendClips(this.audioString, new AudioClip[]
            {
            RandomClip(this.popUpCall)
            });
            //Debug.Log("RPU 1.3");
            int num = offset;
            int num2 = 0;
            //Debug.Log("RPU 1.4");
            while (num < groups.Count && num2 < count)
            {
                //Debug.Log("RPU 1.5");
                ContactGroup contactGroup = groups[num];
                this.AppendPopup(this.audioString, contactGroup.count > 1, contactGroup.globalPos.globalPoint.toVector3, contactGroup.velocity);
                num++;
                num2++;
            }
            CommRadioManager2.PlayMessageString(this.audioString);
        }


        public virtual void AppendClips(List<AudioClip> audioString, params AudioClip[] clips)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                audioString.Add(clips[i]);
            }
        }

        public virtual void AppendPopup(List<AudioClip> audioString, bool grp, Vector3 gPos, Vector3 vel)
        {
            bool flag = WaypointManager.instance && WaypointManager.instance.bullseye;
            if (grp)
            {
                if (flag)
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    RandomClip(this.groupBullseyeClips)
                    });
                }
                else
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    RandomClip(this.groupBraaClips)
                    });
                }
            }
            else if (flag)
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.hostileBullseyeClips)
                });
            }
            else
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.hostileBraaClips)
                });
            }
            Vector3 vector = VTMapManager.GlobalToWorldPoint(new Vector3D(gPos));
            if (flag)
            {
                this.AppendBearing(audioString, WaypointManager.instance.bullseye.position, vector);
                this.AppendRange(audioString, WaypointManager.instance.bullseye.position, vector, true);
            }
            else
            {

                this.AppendBearing(audioString, this.getPlayerPos(), vector);
                this.AppendRange(audioString, this.getPlayerPos(), vector, true);
            }
            this.AppendAltitude(audioString, vector);
            this.AppendAzimuth(audioString, vector, vel);
        }


        public virtual void AppendAzimuth(List<AudioClip> audioString, Vector3 pos, Vector3 vel)
        {
            Vector3 rhs = pos - this.getPlayerPos();
            rhs.Normalize();
            float num = Vector3.Dot(vel.normalized, rhs);
            if (num < -0.8f)
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.hotClips)
                });
            }
            else if (num > 0.5f)
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.coldClips)
                });
            }
            else
            {
                float num2 = VectorUtils.Bearing(Vector3.zero, vel);
                if (num2 > 45f && num2 <= 135f)
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    this.cardinalClips[1]
                    });
                }
                else if (num2 > 135f && num2 <= 225f)
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    this.cardinalClips[2]
                    });
                }
                else if (num2 > 225f && num2 <= 315f)
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    this.cardinalClips[3]
                    });
                }
                else
                {
                    this.AppendClips(audioString, new AudioClip[]
                    {
                    this.cardinalClips[0]
                    });
                }
            }
            if (vel.magnitude > 340f)
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.fastClips)
                });
            }
        }

        public virtual void AppendAltitude(List<AudioClip> audioString, Vector3 pt)
        {
            float num = WaterPhysics.GetAltitude(pt);
            if (num < 1500f)
            {
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.lowClips)
                });
                return;
            }
            this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.Altitude)
                });
            num = this.ConvertedAltitude(num);
            num /= 1000f;
            int num2 = Mathf.RoundToInt(num);
            num2 = Mathf.Max(1, num2);
            AudioClip audioClip;
            if (num2 < 9)
            {
                audioClip = this.compNumber[num2 * 2];
            }
            else if (num2 <= 20)
            {
                audioClip = this.highClips[(num2 - 9) / 2];
            }
            else
            {
                int num3 = 5 + (num2 - 15) / 5;
                num3 = Mathf.Clamp(num3, 0, this.highClips.Length - 1);
                audioClip = this.highClips[num3];
            }
            this.AppendClips(audioString, new AudioClip[]
            {
            audioClip,
            RandomClip(this.alt_Thousand)
            });
        }

        public virtual Vector3 getPlayerPos()
        {
            if (FlightSceneManager.instance && FlightSceneManager.instance.playerActor)
            {
                return FlightSceneManager.instance.playerActor.position;
            }
            return Vector3.zero;
        }

        public virtual void AppendCallsigns(List<AudioClip> audioString)
        {
            //Debug.Log("ACS 0.0");
            if (FlightSceneManager.instance && FlightSceneManager.instance.playerActor)
            {
                //Debug.Log("ACS 1.0");
                this.AppendActorDesignation(audioString, FlightSceneManager.instance.playerActor.designation);
            }
            else
            {
                //Debug.Log("ACS 2.0");
                PhoneticLetters letter = (PhoneticLetters)UnityEngine.Random.Range(0, 25);
                int num = 1;
                int num2 = UnityEngine.Random.Range(1, 10);
                this.AppendActorDesignation(audioString, new Actor.Designation(letter, num, num2));
            }
            
        }

        public virtual void AppendActorDesignation(List<AudioClip> audioString, Actor.Designation designation)
        {
            audioString.Add(this.compPhonetics[(int)designation.letter]);
            this.AppendNumbers(audioString, new int[]
            {
            designation.num1,
            designation.num2
            });
        }

        public virtual void AppendRange(List<AudioClip> audioString, Vector3 fromPt, Vector3 toPt, bool sayMerged = true)
        {
           
            //Debug.Log("From = " + fromPt + "; " +toPt);
            float num = Vector3.ProjectOnPlane(fromPt - toPt, Vector3.up).magnitude;
            //Debug.Log("AR 1.0 Range = " + num);
            if (sayMerged && num < 2000f)
            {
                //Debug.Log("AR 1.1"); 
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.mergedClips)
                });
                return;
            }
            else
            {
                //Debug.Log("AR 1.2");
                this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.Range)
                });
            }
            //Debug.Log("AR 1.3: MMD = " + this.distanceMode);
            num = this.ConvertedDistance(num);
            //Debug.Log("AR 1.4: num = " + num);

            if (this.distanceMode == MeasurementManager.DistanceModes.Meters)
            {
                num /= 1000f;
            }
            float num2 = num;
            num2 = Mathf.Clamp(num2, 0f, 60f);
            //Debug.Log("AR 1.5: num2 = " + num2);

            float f;
            if (num2 <= 10f)
            {
                f = num2;
            }
            else if (num2 <= 20f)
            {
                f = 10f + (num2 - 10f) / 2f;
            }
            else if (num2 <= 40f)
            {
                f = 15f + (num2 - 20f) / 5f;
            }
            else
            {
                f = 19f + (num2 - 40f) / 10f;
            }
            //Debug.Log("AR 1.6: f = " + f);

            int num3 = Mathf.Clamp(Mathf.RoundToInt(f), 0, this.rangeClips.Length - 1);
            //Debug.Log("AR 1.7: num3 = " + num3);

            this.AppendClips(audioString, new AudioClip[]
            {
            this.rangeClips[num3]
            });
            int num4 = rangeTypeVoice(distanceMode);
            //Debug.Log("AR 1.7: num4 = " + num4);
            this.AppendClips(audioString, new AudioClip[]
            {
            this.rangeType[num4]
            });


        }

        private MeasurementManager.DistanceModes distanceMode
        {
            get
            {
                if (MeasurementManager.instance)
                {
                    return MeasurementManager.instance.distanceMode;
                }
                return MeasurementManager.DistanceModes.Meters;
            }
        }

        public MeasurementManager.SpeedModes airspeedMode
        {
            get
            {
                return MeasurementManager.instance.airspeedMode;
            }
            
        }

        public virtual int rangeTypeVoice(MeasurementManager.DistanceModes dmode)
        {
            switch (dmode)
            {
                case MeasurementManager.DistanceModes.Meters:
                    return 0;
                case MeasurementManager.DistanceModes.NautMiles:
                    return 1;
                case MeasurementManager.DistanceModes.Feet:
                    return 2;
                case MeasurementManager.DistanceModes.Miles:
                    return 3;
                default:
                    return 0;
            }
        }
    public virtual float ConvertedAltitude(float f_alt)
        {
            if (MeasurementManager.instance)
            {
                return MeasurementManager.instance.ConvertedAltitude(f_alt);
            }
            return f_alt;
        }
        public virtual float ConvertedDistance(float distance)
        {

            switch (this.distanceMode)
            {
                case MeasurementManager.DistanceModes.Meters:
                    return distance;
                case MeasurementManager.DistanceModes.NautMiles:
                    return MeasurementManager.DistToNauticalMile(distance);
                case MeasurementManager.DistanceModes.Feet:
                    return MeasurementManager.DistToFeet(distance);
                case MeasurementManager.DistanceModes.Miles:
                    return MeasurementManager.DistToMiles(distance);
                default:
                    return distance;
            }
            
        }

        public virtual float ConvertedSpeed(float speed)
        {
            switch (this.airspeedMode)
            {
                case MeasurementManager.SpeedModes.MetersPerSecond:
                    return speed;
                case MeasurementManager.SpeedModes.KilometersPerHour:
                    return MeasurementManager.SpeedToKMH(speed);
                case MeasurementManager.SpeedModes.Knots:
                    return MeasurementManager.SpeedToKnot(speed);
                case MeasurementManager.SpeedModes.MilesPerHour:
                    return MeasurementManager.SpeedToMPH(speed);
                case MeasurementManager.SpeedModes.FeetPerSecond:
                    return MeasurementManager.SpeedToFPS(speed);
                case MeasurementManager.SpeedModes.Mach:
                    return MeasurementManager.SpeedToMach(speed, go.GetComponent<FlightInfo>().altitudeASL);
                default:
                    return -1f;
            }
        }

        public virtual void AppendBearing(List<AudioClip> audioString, Vector3 fromPt, Vector3 toPt)
        {
            this.AppendClips(audioString, new AudioClip[]
                {
                RandomClip(this.Bearing)
                });

            int num = Mathf.RoundToInt(VectorUtils.Bearing(fromPt, toPt));
            int num2 = num / 100 % 10;
            int num3 = num / 10 % 10;
            int num4 = num % 10;
            this.AppendNumbers(audioString, new int[]
            {
            num2,
            num3,
            num4
            });
        }


        public virtual void AppendNumbers(List<AudioClip> audioString, params int[] nums)
        {
            for (int i = 0; i < nums.Length; i++)
            {
                int num = (i == nums.Length - 1) ? (nums[i] * 2 + 1) : (nums[i] * 2);
                audioString.Add(this.compNumber[num]);
            }
        }

        public static GameObject GetChildWithName(GameObject obj, string name, bool check)
        {

            ////Debug.unityLogger.logEnabled = Main.logging;
            Transform[] children = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (check)
                {
                    //Debug.Log("Looking for:" + name + ", Found:" + child.name); 
                }
                if (child.name == name || child.name == (name + "(Clone)") || child.name.Contains(name))
                {
                    return child.gameObject;
                }
            }


            return null;

        }

        public static GameObject GetChildWithExactName(GameObject obj, string name, bool check)
        {

            ////Debug.unityLogger.logEnabled = Main.logging;
            Transform[] children = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (check)
                {
                    //Debug.Log("Looking for:" + name + ", Found:" + child.name);
                }
                if (child.name == name || child.name == (name + "(Clone)"))
                {
                    return child.gameObject;
                }
            }


            return null;

        }
    }
}