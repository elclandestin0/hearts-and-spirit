using System.Collections;
using UnityEngine;

public class UIFader : MonoBehaviour
{
    public CanvasGroup group;
    public float fadeDuration = 1f;

    public IEnumerator FadeOut()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = 1 - (t / fadeDuration);
            yield return null;
        }
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;

    public void FadeIn()
    {
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
    }
}
