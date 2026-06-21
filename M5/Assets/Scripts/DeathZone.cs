using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                Debug.Log("Player entered death zone. Resetting score and returning to main menu.");
                gm.GameOver();
            }
        }
    }
}
