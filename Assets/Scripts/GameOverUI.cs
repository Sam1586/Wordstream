using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI longestWordText;

    [Header("Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    [Header("Animation")]
    [SerializeField] private Animator pageAnimator;
    [SerializeField] private string dropDownTrigger = "DropDown";

    private int pendingScore;
    private string pendingLongestWord = "";

    void Awake()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    void OnEnable()
    {
        ApplyText();
        PlayDropDownAnimation();
    }

    public void Show(int finalScore, string longestWord)
    {
        pendingScore = finalScore;
        pendingLongestWord = longestWord;
        ApplyText();
        gameObject.SetActive(true);
    }

    private void ApplyText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + pendingScore;
        }

        if (longestWordText != null)
        {
            string displayWord = string.IsNullOrEmpty(pendingLongestWord) ? "-" : pendingLongestWord.ToUpper();
            longestWordText.text = "Longest Word: " + displayWord;
        }
    }

    private void PlayDropDownAnimation()
    {
        if (pageAnimator != null && !string.IsNullOrEmpty(dropDownTrigger))
        {
            pageAnimator.ResetTrigger(dropDownTrigger);
            pageAnimator.SetTrigger(dropDownTrigger);
        }
    }

    private void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
