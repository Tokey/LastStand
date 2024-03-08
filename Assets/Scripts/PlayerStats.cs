using Demo.Scripts.Runtime;
using Kinemation.FPSFramework.Runtime.Core.Types;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{

    public float currentHealth;
    //public NetworkVariable<float> currentHealth = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public float maxHealth;

    public ulong ownerID;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }


    [ServerRpc (RequireOwnership = false)]
    public void TakeHitServerRpc(float damage)
    {
        TakeHitClientRpc(damage);
    }

    [ClientRpc]
    public void TakeHitClientRpc(float damage)
    {
        TakeHit(damage);
    }

    public void TakeHit(float damage)
    {
        if (GetComponent<FPSController>().isInvincible)
            return;
        if (currentHealth > 0)
        { 
            currentHealth -= damage;
            GetComponent<FPSController>().takehitTimer = .3f;
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
        }

        if (currentHealth == 0)
        {
            currentHealth = maxHealth;
            if (!this.GetComponent<FPSController>().isInvincible)
            {
                GetComponent<FPSController>().roundDeaths++;
                this.GetComponent<FPSController>().RespawnPlayerServerRpc(); 
            }
        }
    }

  

}
