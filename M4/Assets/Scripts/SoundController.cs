using UnityEngine;

public class SoundController : MonoBehaviour
{

    public AudioSource backgroundMusic;
    public AudioSource pickupSound;
    public AudioSource dropoffSound;

    public void PlayPickupSound()
    {
        pickupSound.Play();
    }
    public void PlayDropoffSound()
    {
        dropoffSound.Play();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        
    }
}
