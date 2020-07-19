﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Harmony;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

public static class AIManager
{
    public static Queue<Packet> AIsToSpawnQueue = new Queue<Packet>();
    private static List<ulong> spawnedAI = new List<ulong>();
    public static List<AI> AIVehicles = new List<AI>(); //This is the list of all AI, and an easy way to access AI variables
    public struct AI
    {
        public CSteamID cSteamID;
        public GameObject vehicle;
        public string vehicleName;
        public Actor actor;
        public ulong vehicleUID;

        public AI(CSteamID cSteamID, GameObject vehicle, string vehicleName, Actor actor, ulong vehicleUID)
        {
            this.cSteamID = cSteamID;
            this.vehicle = vehicle;
            this.vehicleName = vehicleName;
            this.actor = actor;
            this.vehicleUID = vehicleUID;
        }
    }
    /// <summary>
    /// This is used by the host and only the host to spawn ai vehicles.
    /// </summary>
    public static void SpawnAIVehicle(Packet packet) // This should never run on the client
    {
        if (Networker.isHost)
        {
            Debug.LogWarning("Host shouldn't be trying to spawn an ai vehicle.");
            return;
        }
        Message_SpawnAIVehicle message = (Message_SpawnAIVehicle)((PacketSingle)packet).message;

        if (!PlayerManager.gameLoaded)
        {
            Debug.LogWarning("Our game isn't loaded, adding spawn vehicle to queue");
            AIsToSpawnQueue.Enqueue(packet);
            return;
        }
        foreach (ulong id in spawnedAI)
        {
            if (id == message.networkID)
            {
                Debug.Log("Got a spawnAI message for a vehicle we have already added! Returning....");
                return;
            }
        }

        spawnedAI.Add(message.networkID);
        Debug.Log("Got a new aiSpawn uID.");
        if (message.unitName == "Player")
        {
            Debug.LogWarning("Player shouldn't be sent to someones client....");
            return;
        }
        Debug.Log("Trying to spawn AI " + message.aiVehicleName);

        GameObject prefab = UnitCatalogue.GetUnitPrefab(message.unitName);
        if (prefab == null) {
            Debug.LogError(message.unitName + " was not found.");
            return;
        }
        GameObject newAI = GameObject.Instantiate(prefab);
        Debug.Log("Setting vehicle name");
        newAI.name = message.aiVehicleName;
        Actor actor = newAI.GetComponent<Actor>();
        Debug.Log($"Spawned new vehicle at {newAI.transform.position}");

        UIDNetworker_Receiver uidReciever = newAI.AddComponent<UIDNetworker_Receiver>();
        uidReciever.networkUID = message.networkID;

        if (newAI.GetComponent<Rigidbody>() != null) {
            RigidbodyNetworker_Receiver rbNetworker = newAI.AddComponent<RigidbodyNetworker_Receiver>();
            rbNetworker.networkUID = message.networkID;
        }

        if (actor.role == Actor.Roles.Air)
        {
            PlaneNetworker_Receiver planeReceiver = newAI.AddComponent<PlaneNetworker_Receiver>();
            planeReceiver.networkUID = message.networkID;
            AIPilot aIPilot = newAI.GetComponent<AIPilot>();
            aIPilot.kPlane.SetToKinematic();
            aIPilot.kPlane.enabled = false;
            aIPilot.commandState = AIPilot.CommandStates.Override;
            aIPilot.kPlane.enabled = true;
            aIPilot.kPlane.SetVelocity(Vector3.zero);
            aIPilot.kPlane.SetToDynamic();

            RotationToggle wingRotator = aIPilot.wingRotator;
            if (wingRotator != null)
            {
                WingFoldNetworker_Receiver wingFoldReceiver = newAI.AddComponent<WingFoldNetworker_Receiver>();
                wingFoldReceiver.networkUID = message.networkID;
                wingFoldReceiver.wingController = wingRotator;
            }
            if (aIPilot.isVtol)
            {
                Debug.Log("Adding Tilt Controller to this vehicle " + message.networkID);
                EngineTiltNetworker_Receiver tiltReceiver = newAI.AddComponent<EngineTiltNetworker_Receiver>();
                tiltReceiver.networkUID = message.networkID;
            }
        }

        Rigidbody rb = newAI.GetComponent<Rigidbody>();
        Health health = newAI.GetComponent<Health>();
        health.invincible = false;

        foreach (Collider collider in newAI.GetComponentsInChildren<Collider>())
        {
            if (collider)
            {
                collider.gameObject.layer = 9;
            }
        }
        Debug.Log($"Changing {newAI.name}'s position and rotation\nPos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
        rb.interpolation = RigidbodyInterpolation.None;
        rb.position = message.position.toVector3;
        rb.rotation = Quaternion.Euler(message.rotation.toVector3);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Debug.Log($"Finished changing {newAI.name}\n Pos:{rb.position} Rotation:{rb.rotation.eulerAngles}");
        Debug.Log("Doing weapon manager shit on " + newAI.name + ".");
        WeaponManager weaponManager = newAI.GetComponent<WeaponManager>();
        if (weaponManager == null)
            Debug.LogError(newAI.name + " does not seem to have a weapon maanger on it.");
        else
        {
            string[] hpLoadoutNames = new string[30];
            Debug.Log("foreach var equip in message.hpLoadout");
            int debugInteger = 0;
            foreach (var equip in message.hpLoadout)
            {
                Debug.Log(debugInteger);
                hpLoadoutNames[equip.hpIdx] = equip.hpName;
                debugInteger++;
            }
            Debug.Log("Setting Loadout on this new vehicle spawned");
            for (int i = 0; i < hpLoadoutNames.Length; i++)
            {
                Debug.Log("HP " + i + " Name: " + hpLoadoutNames[i]);
            }
            Debug.Log("Now doing loadout shit.");
            Loadout loadout = new Loadout();
            loadout.normalizedFuel = message.normalizedFuel;
            loadout.hpLoadout = hpLoadoutNames;
            loadout.cmLoadout = message.cmLoadout;
            weaponManager.EquipWeapons(loadout);
            weaponManager.RefreshWeapon();
            Debug.Log("Refreshed this weapon manager's weapons.");
            MissileNetworker_Receiver lastReciever;
            for (int i = 0; i < 30; i++)
            {
                int uIDidx = 0;
                HPEquippable equip = weaponManager.GetEquip(i);
                if (equip is HPEquipMissileLauncher)
                {
                    Debug.Log(equip.name + " is a missile launcher");
                    HPEquipMissileLauncher hpML = equip as HPEquipMissileLauncher;
                    Debug.Log("This missile launcher has " + hpML.ml.missiles.Length + " missiles.");
                    for (int j = 0; j < hpML.ml.missiles.Length; j++)
                    {
                        Debug.Log("Adding missile reciever");
                        lastReciever = hpML.ml.missiles[j].gameObject.AddComponent<MissileNetworker_Receiver>();
                        foreach (var thingy in message.hpLoadout) // it's a loop... because fuck you!
                        {
                            Debug.Log("Try adding missile reciever uID");
                            if (equip.hardpointIdx == thingy.hpIdx)
                            {
                                if (uIDidx < thingy.missileUIDS.Length)
                                {
                                    lastReciever.networkUID = thingy.missileUIDS[uIDidx];
                                    lastReciever.thisML = hpML.ml;
                                    lastReciever.idx = j;
                                    uIDidx++;
                                }
                            }

                            else if (equip is HPEquipGunTurret)
                            {   //dear god when this gets cleaned up move to PlaneEquippableManager - Cheese whenever he wrote that comment
                                TurretNetworker_Receiver reciever = equip.gameObject.AddComponent<TurretNetworker_Receiver>();
                                reciever.networkUID = message.networkID;
                                reciever.turret = equip.GetComponent<ModuleTurret>();
                                equip.enabled = false;
                            }
                        }
                    }
                }
            }
            FuelTank fuelTank = newAI.GetComponent<FuelTank>();
            if (fuelTank == null)
                Debug.LogError("Failed to get fuel tank on " + newAI.name);
            fuelTank.startingFuel = loadout.normalizedFuel * fuelTank.maxFuel;
            fuelTank.SetNormFuel(loadout.normalizedFuel);
        }
        AIVehicles.Add(new AI(new CSteamID(message.networkID), newAI, message.aiVehicleName, actor, message.networkID));
        Debug.Log("Spawned in AI " + newAI.name);
    }
    /// <summary>
    /// Tell the connected clients about all the vehicles the host has. This code should never be run on a client.
    /// </summary>
    public static void TellClientAboutAI(CSteamID steamID)
    {
        if (!Networker.isHost)
        {
            Debug.LogWarning("The client shouldn't be trying to tell everyone about AI");
            return;
        }
        Debug.Log("Trying sending AI's to client " + steamID);
        PlaneNetworker_Sender lastPlaneSender;
        RigidbodyNetworker_Sender lastRigidSender;
        foreach (var actor in TargetManager.instance.allActors)
        {
            List<HPInfo> hPInfos = new List<HPInfo>();
            if (actor == null)
                continue;
            if (!actor.isPlayer)
            {
                Debug.Log("Try sending ai " + actor.name + " to client.");
                if (actor.gameObject.GetComponent<UIDNetworker_Sender>() != null)
                {
                    UIDNetworker_Sender uidSender = actor.gameObject.GetComponent<UIDNetworker_Sender>();
                    //lastPlaneSender = actor.gameObject.GetComponent<PlaneNetworker_Sender>();
                    //lastRigidSender = actor.gameObject.GetComponent<RigidbodyNetworker_Sender>();
                    //if (actor.weaponManager != null)
                    //{ hPInfos = VTOLVR_Multiplayer.PlaneEquippableManager.generateHpInfoListFromWeaponManager(actor.weaponManager, VTOLVR_Multiplayer.PlaneEquippableManager.HPInfoListGenerateNetworkType.sender); }
                    //CountermeasureManager cm;
                    //cm = actor.gameObject.GetComponent<CountermeasureManager>();
                    //List<int> cmLoadout = new List<int>();
                    //if (cm)
                    //{
                    //    cmLoadout = VTOLVR_Multiplayer.PlaneEquippableManager.generateCounterMeasuresFromCmManager(cm);
                    //}

                    HPInfo[] hPInfos2 = new HPInfo[0];
                    int[] cmLoadout = new int[0];

                    Debug.Log("Finally sending AI " + actor.name + " to client " + steamID);
                    Networker.SendP2P(steamID, new Message_SpawnAIVehicle(actor.name, actor.unitSpawn.unitName, VTMapManager.WorldToGlobalPoint(actor.gameObject.transform.position), new Vector3D(actor.gameObject.transform.rotation.eulerAngles), uidSender.networkUID, hPInfos2, cmLoadout, 0.65f), EP2PSend.k_EP2PSendReliable);

                }
                else
                {
                    Debug.Log("Could not find the UIDNetworker_Sender");
                }
            }
        }
    }
}
