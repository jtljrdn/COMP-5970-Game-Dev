using UnityEngine;

public class SpeedBoostZone : MonoBehaviour
{
    float boostSpeed = 15f;
    float boostDuration = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerMovement>(out var playerMovement))
            {
                playerMovement.ActivateSpeedBoost(boostSpeed, boostDuration);
            }
        }
    }
}
