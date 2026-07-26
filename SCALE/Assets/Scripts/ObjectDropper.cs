using System.Collections;
using UnityEngine;

// A chute that dispenses a physics object. Each dispense despawns whatever it
// dropped last, so a chamber can always be reset by pressing the button again
// instead of filling up with abandoned cubes.
public class ObjectDropper : MonoBehaviour
{
    [Tooltip("Prefab dropped down the chute. Usually PhysicObject.")]
    public GameObject objectPrefab;

    [Tooltip("Where the object appears. Leave empty to use the child named Spawner.")]
    public Transform spawnPoint;

    [Tooltip("Spawn the object aligned to the spawn point instead of upright. The chute model is flipped, so upright is normally what you want.")]
    public bool matchSpawnRotation = false;

    [Tooltip("Seconds between the button press and the object appearing.")]
    public float spawnDelay = 0f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the AudioSource on this object. The clip is optional - the dropper works silently without it.")]
    public AudioSource audioSource;

    [Tooltip("Played when an object is dispensed.")]
    public AudioClip dispenseClip;

    // The object currently in play, if it hasn't been destroyed.
    public GameObject Current { get; private set; }

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform.Find("Spawner");
        }

        if (spawnPoint == null)
        {
            // Falling back to the chute itself is better than refusing to drop
            // anything, but it will spawn inside the geometry.
            Debug.LogWarning($"{name}: no spawn point assigned, dropping from the chute's origin.", this);
            spawnPoint = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void Dispense()
    {
        if (objectPrefab == null)
        {
            Debug.LogWarning($"{name}: no prefab assigned, nothing to dispense.", this);
            return;
        }

        if (spawnDelay > 0f)
        {
            StartCoroutine(DispenseAfterDelay());
            return;
        }

        DispenseNow();
    }

    // Removes the current object without dropping a replacement.
    public void Clear()
    {
        if (Current != null)
        {
            Destroy(Current);
            Current = null;
        }
    }

    private IEnumerator DispenseAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        DispenseNow();
    }

    private void DispenseNow()
    {
        Clear();

        Quaternion rotation = matchSpawnRotation ? spawnPoint.rotation : Quaternion.identity;

        // Left unparented on purpose: the chute is rotated and a chamber prefab may
        // be scaled, and either would ride along into the object's transform and
        // fight Scalable.
        Current = Instantiate(objectPrefab, spawnPoint.position, rotation);

        if (audioSource != null && dispenseClip != null)
        {
            audioSource.PlayOneShot(dispenseClip);
        }
    }
}
