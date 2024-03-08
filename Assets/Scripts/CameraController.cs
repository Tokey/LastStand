using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    public GameObject cameraHolder;
    // Start is called before the first frame update
    private void Start()
    {
        if (IsOwner)
            cameraHolder.SetActive(true);
        else
            cameraHolder.SetActive(false); 
    }
}
