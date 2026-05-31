using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{

    public GameObject apple;
    int totalApples;
    int collectedApples = 0;
    public TextMeshProUGUI scoreText;
    public bool canFinish = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the total number of apples in the scene
        totalApples = GameObject.FindGameObjectsWithTag("Apple").Length;
        // Update the score text to show the total number of apples
        scoreText.text = "Apples: " + collectedApples + "/" + totalApples;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void HandleCollection(GameObject apple)
    {
        collectedApples++;
        Destroy(apple);

        // Update the score text
        scoreText.text = "Apples: " + collectedApples + "/" + totalApples;

        if (collectedApples >= totalApples)
        {
            Debug.Log("All apples collected!");
            canFinish = true;
        }
    }
}
