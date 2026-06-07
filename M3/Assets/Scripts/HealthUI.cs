using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    public List<Image> hearts = new();

    public void UpdateHealth(int current)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].enabled = i < current;
        }
    }
}