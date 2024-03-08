using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ParticleEffectCleanup : NetworkBehaviour
{
    public float lifeTime;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       lifeTime-= Time.deltaTime;
        if(lifeTime < 0)
            DestroyParticleServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void DestroyParticleServerRpc()
    {
        this.GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
}
