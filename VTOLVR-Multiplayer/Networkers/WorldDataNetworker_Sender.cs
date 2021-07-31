using UnityEngine;

class WorldDataNetworker_Sender : MonoBehaviour
{

    private Message_WorldData lastMessage;
    float lastTimeScale;
    float tick = 0;
    private void Awake()
    {
        lastTimeScale = Time.timeScale;
        lastMessage = new Message_WorldData(lastTimeScale);
    }

    void FixedUpdate()
    {

        float curTimeScale = Time.timeScale;
        tick += Time.fixedDeltaTime;
     if(tick>0.5f)
        {
            tick = 0.0f;
            lastMessage.timeScale = curTimeScale;
            if (Networker.isHost)
            {
                //Debug.Log($"Sending the timescale {lastMessage.timeScale}");
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            }

          
        }
         
        
       
    }

    public void OnDisconnect(Packet packet)
    {
        // If the player disconnects force the timescale back to 1 other wise they get stuck.

        lastMessage.timeScale = 1f;
        if (Networker.isHost)
        {
            //Debug.Log($"Host Disconnecting - Setting timescale to {lastMessage.timeScale}");
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        }

        Message_Disconnecting message = ((PacketSingle)packet).message as Message_Disconnecting;
        Destroy(gameObject);
    }

}
