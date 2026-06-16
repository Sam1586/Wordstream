using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class FadeTo : MonoBehaviour
{
    public Image fadePanel;

    public float duration = 1f;
    public float fadeInDuration = 1f;

    public string SceneToLoadFirst;
    public string SceneToLoadSecond;

    public int switchScene = 0;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        if (fadePanel != null)
        {
            fadePanel.raycastTarget = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingsMenu.EscapeClosedSettingsThisFrame() || SettingsMenu.AnySettingsMenuOpen())
            {
                return;
            }

            if (switchScene == 1)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                Application.Quit();
            }
        }
    }

    public void Fade()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        fadePanel.raycastTarget = true;

        Color color = fadePanel.color;
        float startAlpha = color.a;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            fadePanel.color = color;
            yield return null;
        }

        color.a = 1f;
        fadePanel.color = color;

        yield return StartCoroutine(ChangeSceneWithFade());
    }

    IEnumerator FadeIn()
    {
        Color color = fadePanel.color;
        float startAlpha = color.a;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / fadeInDuration);
            fadePanel.color = color;
            yield return null;
        }

        color.a = 0f;
        fadePanel.color = color;
        fadePanel.raycastTarget = false;
    }

    IEnumerator ChangeSceneWithFade()
    {
        AsyncOperation asyncLoad;

        if (switchScene == 0)
        {
            asyncLoad = SceneManager.LoadSceneAsync(SceneToLoadFirst);
            switchScene = 1;
        }
        else
        {
            asyncLoad = SceneManager.LoadSceneAsync(SceneToLoadSecond);
            switchScene = 0;
        }

        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(FadeIn());
    }
}
