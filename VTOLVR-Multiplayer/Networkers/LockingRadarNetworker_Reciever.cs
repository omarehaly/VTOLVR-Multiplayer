using UnityEngine;

using System.Collections.Generic;
class LockingRadarNetworker_Receiver : MonoBehaviour
{
    private ulong _networkUID;
    private Message_RadarUpdate lastRadarMessage;
    private Message_LockingRadarUpdate lastLockingMessage;
    private LockingRadar lockingRadar;
    private RadarLockData radarLockData;
    private ulong lastLock;
    private bool lastLocked;
    private Actor lastActor;
    private bool disable;
    public static Dictionary<ulong, List<LockingRadarNetworker_Receiver>> recieverDict = new Dictionary<ulong, List<LockingRadarNetworker_Receiver>>();

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
                List<LockingRadarNetworker_Receiver> newList = new List<LockingRadarNetworker_Receiver>();
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
        lockingRadar = gameObject.GetComponentInChildren<LockingRadar>();
        if (lockingRadar == null)
        {
            Debug.Log($"Locking radar on networkUID {networkUID} is null.");
            return;
        }
        lockingRadar.radar = gameObject.GetComponentInChildren<Radar>();
        if (lockingRadar.radar == null)
        {
            Debug.Log($"Radar was null on network uID {networkUID}");
        }
        // lockingRadar.debugRadar = true;
        lastRadarMessage = new Message_RadarUpdate(false, 0, networkUID);
       
     
        
    }

    static public void RadarUpdate(Packet packet)
    {
        Message_RadarUpdate lastRadarMessage = (Message_RadarUpdate)((PacketSingle)packet).message;
        // Debug.Log("Got a new radar update intended for id " + lastRadarMessage.UID);
        List<LockingRadarNetworker_Receiver> plnl = null;
        if (!recieverDict.TryGetValue(lastRadarMessage.UID, out plnl))
            return;
        foreach (var pln in plnl)
        {
            if (lastRadarMessage.UID != pln.networkUID)
                return;

           // Debug.Log($"Doing radarupdate for uid {networkUID}");
            pln.lockingRadar.radar.radarEnabled = lastRadarMessage.on;
            pln.lockingRadar.radar.sweepFov = lastRadarMessage.fov;
        }
    }
    static public void LockingRadarUpdate(Packet packet)
    {


        Message_LockingRadarUpdate lastLockingMessage = (Message_LockingRadarUpdate)((PacketSingle)packet).message;
        // Debug.Log("Got a new locking radar update intended for id " + lastLockingMessage.senderUID);

        List<LockingRadarNetworker_Receiver> plnl = null;
        if (!recieverDict.TryGetValue(lastLockingMessage.senderUID, out plnl))
            return;
        foreach (var pln in plnl)
        {
            if (lastLockingMessage.senderUID != pln.networkUID)
                return;
            if (pln.lockingRadar == null)
            {
                //Debug.Log($"Locking radar on networkUID {networkUID} is null.");
                return;
            }
            if (pln.lockingRadar.radar == null)
            {
                pln.lockingRadar.radar = pln.gameObject.GetComponentInChildren<Radar>();
                if (pln.lockingRadar.radar == null)
                {
                    return;
                    //Debug.Log($"Radar was null on network uID {networkUID}");
                }
            }
            if (!pln.lockingRadar.radar.radarEnabled)
            {
                pln.lockingRadar.radar.radarEnabled = true;
            }
            //Debug.Log($"Doing LockingRadarupdate for uid {networkUID} which is intended for uID {lastLockingMessage.senderUID}");
            if (!lastLockingMessage.isLocked && pln.lockingRadar.IsLocked())
            {
                //Debug.Log("Unlocking radar " + gameObject.name);
                pln.lockingRadar.Unlock();
                pln.lastLock = 0;
                pln.lastLocked = false;
                return;
            }
            else if (lastLockingMessage.actorUID != pln.lastLock || (lastLockingMessage.isLocked && !pln.lockingRadar.IsLocked()))
            {
                // Debug.Log("Trying to lock radar.");

                if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastLockingMessage.actorUID, out pln.lastActor))
                {
                    if (pln.lastActor == null) return;
                    //Debug.Log($"Radar {networkUID} found its lock " + lastActor.name + $" with an id of {lastLock} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                    //else
                    //Debug.Log($"Radar " + gameObject.name + " found its lock " + lastActor.name + $" with an id of {lastLock} while trying to lock id {lastLockingMessage.actorUID}. Trying to force a lock.");
                    pln.lockingRadar.ForceLock(pln.lastActor, out pln.radarLockData);
                    pln.lastLock = lastLockingMessage.actorUID;
                    pln.lastLocked = true;
                    // Debug.Log($"The lock data is Locked: {radarLockData.locked}, Locked Actor: " + radarLockData.actor.name);
                }
                else
                {
                    // Debug.Log($"Could not resolve a lock on uID {lastLockingMessage.actorUID} from sender {lastLockingMessage.senderUID}.");
                }
            }
        }
    }
    static public void OnRadarDetectedActor(Packet packet)
    {
        Message_RadarDetectedActor message = (Message_RadarDetectedActor)((PacketSingle)packet).message;
          List<LockingRadarNetworker_Receiver> plnl = null;
        if (!recieverDict.TryGetValue(message.senderUID, out plnl))
            return;
        foreach (var pln in plnl)
        {
            if (message.senderUID != pln.networkUID)
                return;
            if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(message.detectedUID, out Actor actor))
            {
                if (actor == null)
                {
                    Debug.LogError("Actor is null.");
                    return;
                }
                if (pln.lockingRadar != null)
                    pln.lockingRadar.radar.ForceDetect(actor);
            }
        }
    }
    /*private void FixedUpdate()
    {
        if (lastLocked && !lockingRadar.IsLocked() && lastLock != 0)
        {
            if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastLock, out lastActor))
            {
                Debug.Log("Radar " + gameObject.name + $" refound its lock after dropping it at  {lastLock} while trying to relock id {lastLockingMessage.actorUID}. Last locked: {lastLocked}, lockingRadar.isLocked {lockingRadar.IsLocked()}, lastLock: {lastLock}. Trying to force a lock.");
                lockingRadar.ForceLock(lastActor, out radarLockData);
                // lastLocked = true;
                Debug.Log($"The lock data is Locked: {radarLockData.locked}, reLocked Actor: " + radarLockData.actor.name);
            }
        }
        else if (!lastLocked && lockingRadar.IsLocked())
        {
            Debug.Log($"Radar is locked when it shouldn't be, unlocking. LastLocked: {lastLocked}, lockingRadar.IsLocked() {lockingRadar.IsLocked()}");
            lockingRadar.Unlock();
        }
    }*/
    public void OnDestroy()
    {
     if (recieverDict.ContainsKey(_networkUID))
        {
            recieverDict[networkUID].Remove(this);
        }
        
        Debug.Log("Radar update and Locking Radar update destroyed");
        Debug.Log(gameObject.name);
    }
}
