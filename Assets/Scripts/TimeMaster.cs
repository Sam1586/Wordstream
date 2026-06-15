using UnityEngine;
using UnityEngine.UI;

public class TimeMaster : MonoBehaviour
{
    [Header("References")]
    public RectTransform dialImage;
    public Timer timer;

    [Header("Time Angle Ranges")]
    public float minTime = 0f;
    public float maxTime = 30f;

    public static TimeMaster Instance;

    [Header("Dial Angle Range")]
    [Tooltip("Angle when timeRemaining == minTime")]
    public float minAngle = 60f;

    [Tooltip("Angle when timeRemaining == maxTime")]
    public float maxAngle = 210f;

    [Header("Options")]
    public bool clampTime = true;
    public bool useLocalRotation = true;

    public bool countingDown = false;
    public float timeRemaining = 30f;
    public float maxTimeRemaining = 60f;

    [Header("Info")]
    [SerializeField] private int health = 3;
    public bool timeBelowZero = false;

    void Reset()
    {
        dialImage = GetComponent<RectTransform>();
    }

    public void ReduceHealth(){
        health--;
    }

    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    

        if (dialImage == null)
        {
            dialImage = GetComponent<RectTransform>();
        }

        ResetTime();
    }

    void Update()
    {
        if(countingDown){
            SubtractTime();
        }


        if (timeRemaining <= 0f)
        {
            timeBelowZero = true;
            timeRemaining = 0f;
        }


        UpdateDial();
        timer.UpdateTimerText(timeRemaining);
    }

    public void ResetTime(){
        timeRemaining = maxTime;
    }

    public void SubtractTime(){
        timeRemaining -= Time.deltaTime;
    }

    public void StartCountdown(){
        timeRemaining = maxTime;
        countingDown = true;
    }

    public void AddTime(float wordLength, float wordValue, float bonusMultiplier){

        countingDown = true;

        float timeToAdd = (wordValue * (wordLength * 0.5f) * 2 * bonusMultiplier);

        timeRemaining += timeToAdd;
        Debug.Log("wordValue: " + wordValue + " bonusMultiplier: " + bonusMultiplier + " timeToAdd: " + timeToAdd);

        if (timeRemaining > maxTimeRemaining)
        {
            timeRemaining = maxTimeRemaining;
        }
    }


    public void UpdateDial()
    {

        if (dialImage == null) return;

        float currentTime = timeRemaining;

        if (clampTime)
        {
            currentTime = Mathf.Clamp(currentTime, minTime, maxTime);
        }

        float t = Mathf.InverseLerp(minTime, maxTime, currentTime);
        float angle = Mathf.Lerp(maxAngle, minAngle, t);

        if (useLocalRotation)
        {
            dialImage.localEulerAngles = new Vector3(0f, 0f, angle);
        }
        else
        {
            dialImage.eulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    public void SetTimeRemaining(float newTime)
    {
        timeRemaining = newTime;
        UpdateDial();
    }

    public void SetDialInstant(float newTime, float newMinAngle, float newMaxAngle)
    {
        timeRemaining = newTime;
        minAngle = newMinAngle;
        maxAngle = newMaxAngle;
        UpdateDial();
    }
}