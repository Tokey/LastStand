using Demo.Scripts.Runtime;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FootStepPlayer : NetworkBehaviour
{

    public AudioClip[] footstepSFX;
    public AudioClip[] sprintSFX;
    AudioSource footstepSource;
    int footstepCount;
    FPSMovement playerMovement;

    private void Start()
    {
        playerMovement = this.GetComponentInParent<FPSMovement>();
        footstepSource = this.GetComponentInParent<AudioSource>();
        footstepCount = 0;
    }

    public void PlayFootstepSFX()
    {

        if (playerMovement.IsMoving())
        {
            footstepSource.loop = false;
            
            footstepSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            if (playerMovement.MovementState == FPSMovementState.Sprinting)
            {
                footstepSource.volume = UnityEngine.Random.Range(0.1f, 0.2f);
                //Debug.Log("footstep con :: " + footstepCount);
                if(footstepCount%4==0)
                    footstepSource.PlayOneShot(sprintSFX[UnityEngine.Random.Range(0, sprintSFX.Length - 1)]);

                footstepCount++;
                if (footstepCount > 5000)
                    footstepCount = 0;
            }
            else if(playerMovement.MovementState == FPSMovementState.Walking)
            {
                footstepSource.volume = UnityEngine.Random.Range(0.1f, 0.15f);
                if (footstepCount % 2 == 0)
                    footstepSource.PlayOneShot(footstepSFX[UnityEngine.Random.Range(0, footstepSFX.Length - 1)]);

                footstepCount++;
                if (footstepCount > 5000)
                    footstepCount = 0;
            }
        }

        

    }
}
