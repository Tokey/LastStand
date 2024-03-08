using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayWeponReloadSFX : MonoBehaviour
{

    public AudioClip m4MagInSFX;
    public AudioClip m4MagRemoveSFX;
    public AudioClip m4MagOutSFX;
    public AudioClip m4PinReleaseSFX;
    public AudioClip akChargeSFX;
    public AudioClip akMagInSFX;
    public AudioClip noAmmoSFX;

    public AudioSource audioSource;
    void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
    }

    void M4MagOut()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(m4MagOutSFX);
    }

    void M4MagIn()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(m4MagInSFX);
    }
    void M4MagRemove()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(m4MagRemoveSFX);
    }

    void M4PinRelease()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(m4PinReleaseSFX);
    }

    void AKCharge()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(akChargeSFX);
    }

    void AKMagIn()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(akMagInSFX);
    }

    void NoAmmo()
    {
        audioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(noAmmoSFX);
    }
}
