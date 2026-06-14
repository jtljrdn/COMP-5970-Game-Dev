using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    float timer = 0f;
    public float timeLimit = 60f; // Time limit in seconds
    public CarController carController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartTimer();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer <= 0f)
        {
            Debug.Log("Time's up!");
            carController.GameOver();
            CancelInvoke(nameof(UpdateTimer));
        }
    }

    public void StartTimer()
    {
        timer = timeLimit;
        InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
    }

    private void UpdateTimer()
    {
        timer -= 1f;
        timerText.text = $"{timer:F0}s";
    }
}
