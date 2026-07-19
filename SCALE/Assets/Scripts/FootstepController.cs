using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepController : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip[] footstepClips;
    public float stepInterval = 0.5f;
    
    [Header("Pitch Variation (Removes Robot Sound)")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    private AudioSource audioSource;
    private CharacterController characterController;
    private float stepTimer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (characterController.isGrounded && characterController.velocity.magnitude > 0.1f)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f; 
        }
    }

    void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;

        int randomIndex = Random.Range(0, footstepClips.Length);
        audioSource.clip = footstepClips[randomIndex];
        
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.Play();
    }
}