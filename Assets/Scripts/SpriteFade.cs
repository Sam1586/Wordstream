using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.U2D;
using UnityEngine.Rendering.Universal; 

public class SpriteFade : MonoBehaviour
{
    public float fadeTime = 0.5f;
    private SpriteRenderer sr;
    
    public Light2D spotlight;
    public float initialIntensity;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(FadeCoroutine());
        spotlight.intensity = initialIntensity;
    }

    IEnumerator FadeCoroutine()
    {
        
        float elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color color2 = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
            spotlight.intensity = Mathf.Lerp(initialIntensity, 0, elapsed / fadeTime);
            sr.color = color2;
            yield return null;
        }

        Destroy(gameObject);
        yield return null;
    }
    
}
