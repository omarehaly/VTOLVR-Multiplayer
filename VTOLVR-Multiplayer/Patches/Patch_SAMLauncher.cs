using Harmony;
using UnityEngine;

public static class SAMHelper
{
    public static ulong SAMmissile;
}

[HarmonyPatch(typeof(SAMLauncher), "FireMissileRoutine")]
class Patch9
{
    [HarmonyPrefix]
    public static bool Prefix(SAMLauncher __instance)
    {
        if (!Networker.isHost)
        {
            return true;
        }
        DebugCustom.Log("Beginning sam launch prefix.");
        int j = 0;
        Missile[] missiles = (Missile[])Traverse.Create(__instance).Field("missiles").GetValue();
        for (int i = 0; i < missiles.Length; i++)
        {
            if (missiles[i] != null)
            {
                DebugCustom.Log("Found a suitable missile to attach a sender to.");
                MissileNetworker_Sender missileSender = missiles[i].gameObject.AddComponent<MissileNetworker_Sender>();
                missileSender.networkUID = Networker.GenerateNetworkUID();
                SAMHelper.SAMmissile = missileSender.networkUID;
                return true;
            }
        }
        DebugCustom.Log("Could not find a suitable missile to attach a sender to.");
        SAMHelper.SAMmissile = 0;
        return true;
        // __state = 0;
    }
    [HarmonyPostfix]
    public static void Postfix(SAMLauncher __instance, RadarLockData lockData)
    {
        if (Networker.isHost)
        {
            DebugCustom.Log("A sam has fired, attempting to send it to the client in postfix method.");
            if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(__instance.actor, out ulong senderUID))
            {
                if (VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.TryGetValue(lockData.actor, out ulong actorUID))
                {
                    DebugCustom.Log($"Sending sam launch with a missile uID of {SAMHelper.SAMmissile}, sender uID will be {senderUID}, and the actorUID will be {actorUID}.");
                    NetworkSenderThread.Instance.SendPacketAsHostToAllClients(new Message_SamUpdate(actorUID, SAMHelper.SAMmissile, senderUID), Steamworks.EP2PSend.k_EP2PSendReliable);
                    SAMHelper.SAMmissile = 0;
                }
                else
                    DebugCustom.LogWarning($"Could not resolve SAMLauncher {senderUID}'s target.");
            }
            else
                DebugCustom.LogWarning($"Could not resolve a SAMLauncher's uid.");
        }
    }
}
