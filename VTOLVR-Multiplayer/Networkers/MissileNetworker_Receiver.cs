using Harmony;
using System.Collections.Generic;
using UnityEngine;

using VTOLVR_Multiplayer;
public class MissileNetworker_Receiver : MonoBehaviour
{
  
    public ulong ownerNetworkUID;
    public Missile thisMissile;
    public MissileLauncher thisML;
    public int idx;
    private Message_MissileUpdate lastMessage;
    private Traverse traverseML;
    private Traverse traverseMSL;
    private RadarLockData lockData;
    // private Rigidbody rigidbody; see missileSender for why i not using rigidbody
    private bool hasFired = false;
    private bool exploded = false;
    private List<int> colliderLayers = new List<int>();
    bool started = false;
    public static List<Actor> radarMissiles = new List<Actor>();

    public static Dictionary<ulong, List<MissileNetworker_Receiver>> recieverDict = new Dictionary<ulong, List<MissileNetworker_Receiver>>();
    private ulong _networkUID;
    public ulong networkUID
    {
        get
        {
            return _networkUID;
        }
        set
        {
           
            if (recieverDict.ContainsKey(networkUID))
            {
                recieverDict[networkUID].Remove(this);
            }
            if (!recieverDict.ContainsKey(value))
            {
                List<MissileNetworker_Receiver> newList = new List<MissileNetworker_Receiver>();
                recieverDict.Add(value, newList);
                newList.Add(this);
            }
            else
            {
                recieverDict[value].Add(this);
            }

            this._networkUID = value;
        }
    }
    private void Awake()
    {
        if (thisMissile == null)
        {
            thisMissile = GetComponent<Missile>();
        }
        started = true;
        // rigidbody = GetComponent<Rigidbody>();
       // Networker.MissileUpdate += MissileUpdate;
        thisMissile.OnDetonate.AddListener(new UnityEngine.Events.UnityAction(() => { DebugCustom.Log("Missile detonated: " + thisMissile.name); }));
        //if (thisMissile.guidanceMode == Missile.GuidanceModes. || thisMissile.guidanceMode == Missile.GuidanceModes.Optical || thisMissile.guidanceMode == Missile.GuidanceModes.GPS)
        {
            foreach (var collider in thisMissile.GetComponentsInChildren<Collider>())
            {
                colliderLayers.Add(collider.gameObject.layer);
                collider.gameObject.layer = 9;
            }
        }

        thisMissile.explodeRadius *= 2.48f;
        traverseML = Traverse.Create(thisML);
        traverseMSL = Traverse.Create(thisMissile);
        traverseMSL.Field("detonated").SetValue(true);
    }

    static public void MissileUpdate(Packet packet)
    {
        Message_MissileUpdate lastMessage = ((PacketSingle)packet).message as Message_MissileUpdate;
        List<MissileNetworker_Receiver> plnl = null;

        if (!recieverDict.TryGetValue(lastMessage.networkUID, out plnl))
            return; foreach (var pln in plnl)
        {
            if (pln == null)
                return;
            if (lastMessage.networkUID != pln.networkUID)
                return;
            if (!pln.thisMissile.gameObject.activeSelf)
        {
                //Debug.LogError(thisMissile.gameObject.name + " isn't active in hiearchy, changing it to active.");
                pln.thisMissile.gameObject.SetActive(true);
        }
        if (pln.started == false)
        {
            return;
        }
        if (pln.traverseML == null)
        {
                pln.traverseML = Traverse.Create(pln.thisML);
        } 
        
        if (!pln.thisMissile.fired)
        {
            //Debug.Log(thisMissile.gameObject.name + " missile fired on one end but not another, firing here.");
            if (pln.thisML == null)
            {
                //Debug.LogError($"Missile launcher is null on missile {thisMissile.actor.name}, someone forgot to assign it.");
            }
            if (lastMessage.guidanceMode == Missile.GuidanceModes.Radar)
            {
                // thisMissile.debugMissile = true;
                RadarMissileLauncher radarLauncher = pln.thisML as RadarMissileLauncher;
                if (!VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastMessage.radarLock, out Actor actor))
                {
                    //Debug.LogWarning($"Could not resolve missile launcher radar lock from uID {lastMessage.radarLock}.");
                }
                else
                {
                    if (radarLauncher.lockingRadar != null)
                        radarLauncher.lockingRadar.ForceLock(actor, out pln.lockData);
                    //else
                        //Debug.LogWarning("Locking Radar null on object " + thisMissile.name);
                }
                if (radarLauncher != null)
                {
                    radarMissiles.Add(pln.thisMissile.actor);
                        //Debug.Log("Guidance mode radar, firing it as a radar missile.");
                        pln.traverseML.Field("missileIdx").SetValue(pln.idx);
                    if (!radarLauncher.TryFireMissile())
                    {
                        DebugCustom.LogError($"Could not fire radar missile, lock data is as follows: Locked: {pln.lockData.locked}, Actor: {pln.lockData.actor}");
                    }
                    else
                    {
                        RigidbodyNetworker_Receiver rbReceiver = pln.gameObject.AddComponent<RigidbodyNetworker_Receiver>();
                        rbReceiver.networkUID = pln.networkUID;
                        foreach(PlayerManager.Player p in PlayerManager.players)
                        {
                            if(p.cSteamID.m_SteamID == pln.ownerNetworkUID)
                            {
                                rbReceiver.playerWeRepresent = p;
                            }
                        }
                       
                        rbReceiver.smoothingTime =0.5f;
                    }
                }
            }
            else
            {
                if (lastMessage.guidanceMode == Missile.GuidanceModes.Heat)
                {
                        //Debug.Log("Guidance mode Heat.");
                        pln.thisMissile.heatSeeker.transform.rotation = lastMessage.seekerRotation;
                        pln.thisMissile.heatSeeker.SetHardLock();
                }

                if (lastMessage.guidanceMode == Missile.GuidanceModes.Optical)
                {
                    //Debug.Log("Guidance mode Optical.");

                    GameObject emptyGO = new GameObject();
                    Transform newTransform = emptyGO.transform;

                    newTransform.position = VTMapManager.GlobalToWorldPoint(lastMessage.targetPosition);
                        pln.thisMissile.SetOpticalTarget(newTransform);
                    //thisMissile.heatSeeker.SetHardLock();

                    if (pln.thisMissile.opticalLOAL)
                    {
                            pln.thisMissile.SetLOALInitialTarget(VTMapManager.GlobalToWorldPoint(lastMessage.targetPosition));

                    }
                }
                    // Debug.Log("Try fire missile clientside");
                    pln.traverseML.Field("missileIdx").SetValue(pln.idx);
                    pln.thisML.FireMissile();
                RigidbodyNetworker_Receiver rbReceiver = pln.gameObject.AddComponent<RigidbodyNetworker_Receiver>();
                rbReceiver.networkUID = pln.networkUID;
                rbReceiver.smoothingTime = 0.5f;
            }
            if (pln.hasFired != pln.thisMissile.fired)
            {
                    //Debug.Log("Missile fired " + thisMissile.name);
                    pln.hasFired = true;

                //AIDictionaries.allActors.Add(networkUID, thisMissile.actor);
                //AIDictionaries.reverseAllActors.Add(thisMissile.actor, networkUID);
                if (pln.colliderLayers.Count > 0)
                        pln.StartCoroutine(pln.colliderTimer());
            }
        }

            //explode missle after it has done its RB physics fixed timestep
            if (!pln.exploded)
                if (lastMessage.hasExploded)
                {

                    DebugCustom.Log("Missile exploded.");
                    if (pln.thisMissile != null)
                    {
                        pln.traverseMSL.Field("detonated").SetValue(false);

                        ///thisMissile.rb.velocity = thisMissile.transform.forward * 10.0f;
                        pln.thisMissile.Detonate();
                    }

                }
        }
    }
    private void LateUpdate()
    {
        if(hasFired)
        if (thisMissile != null)
        {
            if (!exploded)
            {
                traverseMSL.Field("detonated").SetValue(true);
                traverseMSL.Field("radarLostTime").SetValue(0.0f);
                traverseMSL.Field("finalTorque").SetValue(new Vector3(0.0f, 0.0f, 0.0f));
            }
        }

    }


    private System.Collections.IEnumerator colliderTimer()
    {
        yield return new WaitForSeconds(0.75f);
        int count = 0;
        foreach (var collider in thisMissile.GetComponentsInChildren<Collider>())
        {
            collider.gameObject.layer = colliderLayers[count];
            count++;
        }

    }

    public void OnDestroy()
    {
        if (recieverDict.ContainsKey(_networkUID))
        {
            recieverDict[networkUID].Remove(this);
        }
        radarMissiles.Remove(thisMissile.actor); 
    }
}

/* Possiable Issue
 * 
 * A missile on a client may explode early because of the random chance it loses locking bceause of 
 * counter measures. 
 * 
 * Missile.cs Line 633
 * This bool function could return false on a clients game and true on the owners game.
 * No error should occour on either game however the missile would now be out of sync, causing bigger issues.
 * 
 * The reason I have't tried fixing this issue is because it's right in the middle of UpdateTargetData() so
 * it would require a lot of rewriting game code just to add a network check for it.
 * 
 * As of writing this note CMS are not networked so it wouldn't effect it, but later on it will.
 * . Marsh.Mello . 21/02/2020 
 * Temperz87 says you suck.
 */
