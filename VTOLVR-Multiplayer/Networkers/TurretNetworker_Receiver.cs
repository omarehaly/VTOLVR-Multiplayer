using UnityEngine;

using System.Collections.Generic;
class TurretNetworker_Receiver : MonoBehaviour
{
    
    public ulong turretID;
    private Message_TurretUpdate lastMessage;
    public ModuleTurret turret;
    public static Dictionary<ulong, List<TurretNetworker_Receiver>> recieverDict = new Dictionary<ulong, List<TurretNetworker_Receiver>>();
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
                List<TurretNetworker_Receiver> newList = new List<TurretNetworker_Receiver>();
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
        lastMessage = new Message_TurretUpdate(new Vector3D(), networkUID, turretID);
    
        if (turret == null)
        {
            turret = base.GetComponentInChildren<ModuleTurret>();
            if (turret == null)
            {
                Debug.LogError($"Turret was null on ID {networkUID}");
            }
        }
    }

    public static void TurretUpdate(Packet packet)
    {

        Message_TurretUpdate lastMessage = (Message_TurretUpdate)((PacketSingle)packet).message;
        List<TurretNetworker_Receiver> plnl = null;
        if (!recieverDict.TryGetValue(lastMessage.UID, out plnl))
            return;
        foreach (var pln in plnl)
        { 
            if (lastMessage.UID != pln.networkUID)
                return;
        if (lastMessage.turretID != pln.turretID)
            return;

        pln.turret.AimToTargetImmediate(lastMessage.direction.toVector3.normalized * 1000);
         }
   
}

    public void OnDestroy()
    {
        if (recieverDict.ContainsKey(_networkUID))
        {
            recieverDict[networkUID].Remove(this);
        }
        Networker.TurretUpdate -= TurretUpdate;
        Debug.Log("Destroyed TurretUpdate");
        Debug.Log(gameObject.name);
    }
}
