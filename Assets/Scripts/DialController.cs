using UnityEngine;
using UnityEngine.UI;

public class DialController : MonoBehaviour
{
    [Header("References")]
    public RectTransform dialImage;

    [Header("Time Range")]
    public float minTime = 0f;
    public float maxTime = 30f;
    public float timeRemaining = 30f;

    [Header("Dial Angle Range")]
    [Tooltip("Angle when timeRemaining == minTime")]
    public float minAngle = 60f;

    [Tooltip("Angle when timeRemaining == maxTime")]
    public float maxAngle = 210f;

    [Header("Options")]
    public bool clampTime = true;
    public bool useLocalRotation = true;

    [SerializeField] private bool countingDown = false;


    void Reset()
    {
        dialImage = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (dialImage == null)
        {
            dialImage = GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if(countingDown){
            SubtractTime();
        }

        UpdateDial();
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
        float timeToAdd = wordLength * wordValue * bonusMultiplier * 0.2f;

        timeRemaining += timeToAdd;
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
        float angle = Mathf.Lerp(minAngle, maxAngle, t);

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