using UnityEngine;
using System.Collections;

public class BlinkTimer : MonoBehaviour
{
    [SerializeField] private float blinkInterval = 0.5f; // Time between blinks in seconds
    [SerializeField] private Typing typingScript;
    [SerializeField] private MenuTyping menuTypingScript;

    public GameObject[] objects;
    private int index;
    private bool wasTyping;
    private bool blinkingDisabled;
    private Coroutine blinkCoroutine;

    void Start()
    {
        blinkCoroutine = StartCoroutine(Blink());
    }

    void Update()
    {
        if (blinkingDisabled)
        {
            return;
        }

        bool isTyping = WordIsBeingTyped();

        if (isTyping && !wasTyping)
        {
            StopBlinking();
            SetAllObjectsActive(false);
            wasTyping = true;
        }
        else if (!isTyping && wasTyping)
        {
            wasTyping = false;
            blinkCoroutine = StartCoroutine(Blink());
        }
    }

    public void DisableBlinking()
    {
        blinkingDisabled = true;
        StopBlinking();
        SetAllObjectsActive(false);
    }

    IEnumerator Blink()
    {
        while (true)
        {
            if (objects == null || objects.Length == 0)
            {
                yield return null;
                continue;
            }

            index %= objects.Length;

            if (objects[index] == null)
            {
                index = (index + 1) % objects.Length;
                yield return null;
                continue;
            }

            objects[index].SetActive(true);
            yield return new WaitForSeconds(blinkInterval);

            objects[index].SetActive(false);
            index = (index + 1) % objects.Length;
        }
    }

    private bool WordIsBeingTyped()
    {
        return typingScript != null && typingScript.temporaryLetterTiles.Count > 0 ||
               menuTypingScript != null && menuTypingScript.temporaryLetterTiles.Count > 0;
    }

    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private void SetAllObjectsActive(bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }
}
