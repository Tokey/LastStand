using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.EventSystems.EventTrigger;

public class Bullet : NetworkBehaviour
{
    public float bulletSpeed;
    public float bulletLifeTime;
    public float bulletDamage;

    public float bulletDistance;
    public float minimumDistanceToOpponent = float.MaxValue;
    public GameObject ObjectHitEffect;
    public NetworkVariable<ulong> ownerID = new(99, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool despawnInstructionSent = false;
    public string fileNameSuffix = "";
    public String logFileName = "";
    public String hitObjectName = "No Hit";
    public bool enemyHit;
    public bool isHeadshot;

    public float damageDelt;
    public DateTime spawnTime;
    public DateTime destroyTime;
    public float distanceTravelled;
    public Vector3 spawnPosition;
    Vector3 oldPosition;

    public GameObject owningPlayerGO;
    public GameObject enemyGO;
    private void Start()
    {
        despawnInstructionSent = false;
        minimumDistanceToOpponent = float.MaxValue;
        spawnTime = DateTime.Now;
        enemyHit = false;
        distanceTravelled = 0f;
        oldPosition = this.transform.position;
        isHeadshot = false;
        damageDelt = 0;
    }
    // Update is called once per frame
    void Update()
    {
        bulletLifeTime -= Time.deltaTime;
        if (bulletLifeTime < 0)
            DestroyBulletServerRpc();

        Vector3 distanceVector = transform.position - oldPosition;
        distanceTravelled += distanceVector.magnitude;
        oldPosition = transform.position;

        GameObject[] localPlayerObjects = GameObject.FindGameObjectsWithTag("Player");

        if (owningPlayerGO == null || enemyGO == null)
        {
            foreach (var playerObj in localPlayerObjects)
            {
                if (playerObj.GetComponent<FPSController>().playerID.Value == ownerID.Value)
                {
                    owningPlayerGO = playerObj;
                }
                else
                {
                    enemyGO = playerObj;
                }
            }
        }

        if(enemyGO != null)
        {
            if (Vector3.Distance(this.transform.position, enemyGO.transform.position) <minimumDistanceToOpponent)
            {
                minimumDistanceToOpponent = Vector3.Distance(this.transform.position, enemyGO.transform.position);
            }

            if (fileNameSuffix == "")
            {
                fileNameSuffix = enemyGO.GetComponent<FPSController>().fileNameSuffix;
            }

        }

        if (owningPlayerGO != null)
        {
            if (fileNameSuffix == "")
            {
                fileNameSuffix = owningPlayerGO.GetComponent<FPSController>().fileNameSuffix;
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!despawnInstructionSent && !collision.gameObject.name.Equals("MainCharacter(Clone)"))
        {
            despawnInstructionSent = true;
            hitObjectName = collision.gameObject.name;

            PlayerStats pstats = collision.gameObject.GetComponentInParent<PlayerStats>();

            if (pstats != null)
            {
                if (collision.gameObject.GetComponentInParent<FPSController>().playerID.Value != ownerID.Value)
                {
                    enemyHit = true;
                    //TODO: Headshots
                    if (hitObjectName == "Head")
                    {
                        pstats.TakeHitServerRpc(bulletDamage * 2);
                        isHeadshot = true;
                        damageDelt = bulletDamage * 2;
                    }
                    else { 
                        pstats.TakeHitServerRpc(bulletDamage);
                        damageDelt = bulletDamage;
                    }

                    GameObject[] localPlayerObjects = GameObject.FindGameObjectsWithTag("Player");

                    foreach (var playerObj in localPlayerObjects)
                    {
                        if (playerObj.GetComponent<FPSController>().playerID.Value == ownerID.Value)
                        {
                            if (hitObjectName == "Head")
                                playerObj.GetComponent<FPSController>().AddToScoreServerRpc(5);
                            else
                                playerObj.GetComponent<FPSController>().AddToScoreServerRpc(1);
                            break;
                        }
                    }
                }
                else
                {
                    //Self hit
                }
            }
            else
            {
                Debug.Log("PLAYER STATS NULL");
            }
        }
        SpawnParticleServerRpc();
        DestroyBulletServerRpc();

    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnParticleServerRpc()
    {
        GameObject ps = Instantiate(ObjectHitEffect, this.transform.position, this.transform.rotation.normalized);
        ps.GetComponent<NetworkObject>().Spawn();

    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyBulletServerRpc()
    {
        despawnInstructionSent = true;
        this.GetComponent<NetworkObject>().Despawn();
        LogOnDestroy();
    }
    public void LogOnDestroy()
    {
        if (fileNameSuffix == "" || ownerID.Value==99)
        {
            Destroy(this);
            return; 
        }
        destroyTime = DateTime.Now;
        if(ownerID.Value==0)
            logFileName = "Data\\Logs\\ProjectileData_" + fileNameSuffix + "_" + "PrimaryOwner_"+ownerID.Value + ".csv";
        else
            logFileName = "Data\\Logs\\ProjectileData_" + fileNameSuffix + "_" + "ControlOwner_" + ownerID.Value + ".csv";
        TextWriter textWriter = null;

        while (textWriter == null)
            textWriter = File.AppendText(logFileName);

        String tickLogLine =
            destroyTime.ToString() + "," +
            owningPlayerGO.GetComponent<LatencyManager>().currentRoundNumber + "," +
            owningPlayerGO.GetComponent<LatencyManager>().currentRoundTimer.Value + "," +
            owningPlayerGO.GetComponent<LatencyManager>().currentSessionTimer.Value + "," +
            ownerID.Value.ToString() + "," +
            spawnTime.ToString() + "," +
            distanceTravelled + "," +
            minimumDistanceToOpponent.ToString() + "," +
            enemyHit.ToString() + "," +
            hitObjectName + "," +
            spawnPosition.ToString() + "," +
            this.transform.position.ToString() + "," +
            owningPlayerGO.transform.position.ToString() + "," +
            enemyGO.transform.position.ToString() + "," +
            damageDelt + "," +
            isHeadshot
            ;
        //Debug.Log("LOG :: " + tickLogLine);
        textWriter.WriteLine(tickLogLine);
        textWriter.Close();

        Destroy(this);
    }
}
