using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    public void DeactivateRulebookPanel()
    {
        RulebookButton rulebookButton = FindObjectOfType<RulebookButton>();
        if (rulebookButton != null)
        {
            rulebookButton.DeactivateRulebookPanel();
        }
        else
        {
            DisableSelf();
        }
    }
}
