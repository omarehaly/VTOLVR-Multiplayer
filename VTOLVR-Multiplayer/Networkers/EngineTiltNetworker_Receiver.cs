using UnityEngine;

class EngineTiltNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_EngineTiltUpdate lastMessage;
    private TiltController tiltController;

    private void Awake()
    {
        tiltController = GetComponent<TiltController>();
        lastMessage = new Message_EngineTiltUpdate(networkUID, 0);
        Networker.EngineTiltUpdate += EngineTiltUpdate;
    }

    public void EngineTiltUpdate(Packet packet)
    {
        lastMessage = (Message_EngineTiltUpdate)((PacketSingle)packet).message;
        if (lastMessage.networkUID != networkUID)
            return;
        tiltController.SetTiltImmediate(lastMessage.angle);
    }

    public void OnDestroy()
    {
        Networker.EngineTiltUpdate -= EngineTiltUpdate;
        DebugCustom.Log("Destroyed Engine Tilt Update");
        DebugCustom.Log(gameObject.name);
    }
}