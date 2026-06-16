using UnityEngine;
using TMPro;

public class OpacityScript : MonoBehaviour
{
    [Range(0f, 1f)]
    public float opacity = 0.4f;
    
    void Awake()
    {
        ApplyOpacity();
    }

    [ContextMenu("Apply Opacity To Children")]
    private void ApplyOpacity()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (SpriteRenderer sr in sprites)
        {
            Color c = sr.color;
            c.a = opacity;
            sr.color = c;
        }

        foreach (TMP_Text tmp in texts)
        {
            Color c = tmp.color;
            c.a = opacity;
            tmp.color = c;
        }
    }
}
