using UnityEngine;
using Harmony;
using System.Collections;
using System.Collections.Generic;
class ShipNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ShipUpdate lastMessage;
    private float timer;
    public ShipMover ship;

    public List<CarrierCatapult> catapults;
    private void Awake()
    {
        lastMessage = new Message_ShipUpdate(new Vector3D(), new Quaternion(), new Vector3D(), networkUID);
        ship = GetComponent<ShipMover>();
        catapults = new List<CarrierCatapult>();

        foreach (var ctp in GetComponentsInChildren<CarrierCatapult>(true))
        {
            catapults.Add(ctp);
        }
    }

    private IEnumerator CloseDeflector(CarrierCatapult ctp)
    {

        yield return new WaitForSeconds(20.0f);
        ctp.deflectorRotator.SetDefault();
        Traverse.Create(ctp).Field("catapultReady").SetValue(true);
    }
    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > 1)
        {
            timer = 0;

            lastMessage.position = VTMapManager.WorldToGlobalPoint(ship.transform.position);
            lastMessage.rotation = ship.transform.rotation;
            lastMessage.velocity = new Vector3D(ship.velocity);

            lastMessage.UID = networkUID;
            if (Networker.isHost)
                Networker.addToUnreliableSendBuffer(lastMessage);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            foreach (CarrierCatapult ctp in catapults)
            {
                if (ctp.deflectorRotator.deployed)
                {
                    StartCoroutine("CloseDeflector", ctp);
                }
            }
        }
    }
}
