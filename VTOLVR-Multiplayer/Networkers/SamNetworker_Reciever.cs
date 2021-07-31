using Harmony;
using System.Collections.Generic;
using UnityEngine;

class SamNetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    public ulong[] radarUIDS;
    private Message_SamUpdate lastMessage;
    private SAMLauncher samLauncher;
    private RadarLockData lastData;
    private Actor lastActor;
    private void Awake()
    {
        samLauncher = GetComponentInChildren<SAMLauncher>();
        Networker.SAMUpdate += SamUpdate;
        samLauncher.LoadAllMissiles();
        if (samLauncher.lockingRadars == null)
        {

            List<LockingRadar> lockingRadars = new List<LockingRadar>();
            Actor lastActor;
            foreach (var uID in radarUIDS)
            {
                DebugCustom.Log($"Try adding uID {uID} to SAM's radars.");
                if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(uID, out lastActor))
                {
                    DebugCustom.Log("Got the actor.");
                    foreach (var radar in lastActor.gameObject.GetComponentsInChildren<LockingRadar>())
                    {
                        lockingRadars.Add(radar);
                        DebugCustom.Log("Added radar to a sam launcher!");
                    }
                }
                else
                {
                    DebugCustom.LogError($"Could not resolve actor from uID {uID}.");
                }
            }
            samLauncher.lockingRadars = lockingRadars.ToArray();
        }
    }
    private void SamUpdate(Packet packet)
    {
        if (samLauncher.lockingRadars == null)
        {

            List<LockingRadar> lockingRadars = new List<LockingRadar>();
            Actor lastActor;
            foreach (var uID in radarUIDS)
            {
                DebugCustom.Log($"Try adding uID {uID} to SAM's radars.");
                if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(uID, out lastActor))
                {
                    DebugCustom.Log("Got the actor.");
                    foreach (var radar in lastActor.gameObject.GetComponentsInChildren<LockingRadar>())
                    {
                        lockingRadars.Add(radar);
                        DebugCustom.Log("Added radar to a sam launcher!");
                    }
                }
                else
                {
                    DebugCustom.LogError($"Could not resolve actor from uID {uID}.");
                }
            }
            samLauncher.lockingRadars = lockingRadars.ToArray();
        }
        lastMessage = (Message_SamUpdate)((PacketSingle)packet).message;
        if (lastMessage.senderUID != networkUID)
            return;
        DebugCustom.Log("Got a sam update message.");
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastMessage.actorUID, out lastActor))
        {
            foreach (var radar in samLauncher.lockingRadars)
            {
                DebugCustom.Log("Found a suitable radar for this sam.");
                radar.ForceLock(lastActor, out lastData);
                if (lastData.locked)
                {
                    DebugCustom.Log("Beginning sam launch routine for reciever.");
                    int j = 0;
                    Missile[] missiles = (Missile[])Traverse.Create(samLauncher).Field("missiles").GetValue();

                   
                   bool needToLoad = true;
                     for (int i = 0; i < missiles.Length; i++)
                    {
                        if (missiles[i] != null)
                        {
                            needToLoad = false;

                        }
                    }
                     if(needToLoad)
                    {
                        samLauncher.LoadMissile(0);
                    }
                            for (int i = 0; i < missiles.Length; i++)
                    {
                        if (missiles[i] != null)
                        {
                            DebugCustom.Log("Found a suitable missile to attach a reciever to.");
                            MissileNetworker_Receiver missileReciever = missiles[i].gameObject.AddComponent<MissileNetworker_Receiver>();
                            missileReciever.networkUID = lastMessage.missileUID;
                            DebugCustom.Log($"Made new missile receiver with uID {missileReciever.networkUID}");
                            break;
                        }
                    }
                    DebugCustom.Log("Firing sam.");
                    samLauncher.FireMissile(lastData);
                    /*Missile missile = (Missile)Traverse.Create(samLauncher).Field("firedMissile").GetValue();
                    MissileNetworker_Receiver reciever = missile.gameObject.AddComponent<MissileNetworker_Receiver>();
                    reciever.networkUID = lastMessage.missileUID;*/
                    return;
                }
                else
                {
                    DebugCustom.Log("Couldn't force a lock, trying with another radar.");
                }
            }
        }
        else
        {
            DebugCustom.Log($"Could not resolve lock for sam {networkUID}.");
        }
    }

    public void OnDestroy()
    {
        Networker.SAMUpdate -= SamUpdate;
        DebugCustom.Log("Destroyed SamUpdate");
        DebugCustom.Log(gameObject.name);
    }
}
