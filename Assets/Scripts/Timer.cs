using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    public void UpdateTimerText(float timeToDisplay)
    {
        /*
        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);

        timeText.text = $"{minutes}:{seconds:00}";

        */
        
        timeText.text = "" + (int)timeToDisplay;
    }
}
