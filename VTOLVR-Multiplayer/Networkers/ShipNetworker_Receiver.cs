using Harmony;
using UnityEngine;
using System.Collections; 
using System.Collections.Generic;
class ShipNetworker_Receiver : MonoBehaviour
{
     
    private Message_ShipUpdate lastMessage;
    public ShipMover ship;
    public Traverse shipTraverse;

    public float smoothTime = 5f;
    public float rotSmoothTime = 5f;
    public Vector3D targetPositionGlobal;
    public Vector3 targetPosition;
    public Vector3 targetVelocity;
    public Quaternion targetRotation;
    public List<CarrierCatapult> catapults;
    private ulong _networkUID;
    public static Dictionary<ulong, List<ShipNetworker_Receiver>> recieverDict = new Dictionary<ulong, List<ShipNetworker_Receiver>>();

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
                List<ShipNetworker_Receiver> newList = new List<ShipNetworker_Receiver>();
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
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);
     
     
        ship = GetComponent<ShipMover>();
        ship.enabled = false;
        shipTraverse = Traverse.Create(ship);

        catapults = new List<CarrierCatapult>();

        foreach (var ctp in GetComponentsInChildren<CarrierCatapult>(true))
        {
            catapults.Add(ctp);
        }
    }

    void FixedUpdate()
    {
        targetPositionGlobal += targetVelocity * Time.fixedDeltaTime;
        targetPosition = VTMapManager.GlobalToWorldPoint(targetPositionGlobal);
        ship.rb.MovePosition(ship.transform.position + targetVelocity * Time.fixedDeltaTime + ((targetPosition - ship.transform.position) * Time.fixedDeltaTime) / smoothTime);
        ship.rb.velocity = targetVelocity + (targetPosition - ship.transform.position) / smoothTime;
        shipTraverse.Field("_velocity").SetValue(ship.rb.velocity);//makes the wake emit partical
        ship.rb.MoveRotation(Quaternion.Lerp(ship.transform.rotation, targetRotation, Time.fixedDeltaTime / rotSmoothTime));
    }

    public static void ShipUpdate(Packet packet)
    {
        Message_ShipUpdate lastMessage = (Message_ShipUpdate)((PacketSingle)packet).message;
        List<ShipNetworker_Receiver> plnl = null;
        if (!recieverDict.TryGetValue(lastMessage.UID, out plnl))
            return;
        foreach (var pln in plnl)
        {
            if (lastMessage.UID != pln.networkUID)
                return;

            pln.targetPositionGlobal = lastMessage.position + lastMessage.velocity.toVector3 * Networker.pingToHost;
            pln.targetVelocity = lastMessage.velocity.toVector3;
            pln.targetRotation = lastMessage.rotation;

            if ((VTMapManager.GlobalToWorldPoint(lastMessage.position) - pln.ship.transform.position).magnitude > 100)
            {
                Debug.Log("Ship is too far, teleporting. This message should apear once per ship at spawn, if ur seeing more something is probably fucky");
                pln.ship.transform.position = VTMapManager.GlobalToWorldPoint(lastMessage.position);
            }

            foreach (CarrierCatapult ctp in pln.catapults)
            {
                if (ctp.deflectorRotator.deployed)
                {
                    pln.StartCoroutine("CloseDeflector", ctp);
                }
            }
        }
    }

    public void OnDestroy()
    {
        if (recieverDict.ContainsKey(_networkUID))
        {
            recieverDict[networkUID].Remove(this);
        }
        Debug.Log("Destroyed ShipUpdate");
        Debug.Log(gameObject.name);
    }

    private IEnumerator CloseDeflector(CarrierCatapult ctp)
    {
      
        yield return new WaitForSeconds(20.0f);
       ctp.deflectorRotator.SetDefault();
       Traverse.Create(ctp).Field("catapultReady").SetValue(true);
    }
}
