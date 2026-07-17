using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Kötü son: siyah ekrana fade + harf harf son metni.
public class GameEndController : MonoBehaviour
{
    public Image fadeImage;
    public Text endTextLabel;
    public float fadeDuration = 2f;

    public void TriggerEnd(string endText)
    {
        StartCoroutine(EndRoutine(endText));
    }

    IEnumerator EndRoutine(string endText)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true; // arkadaki UI'ı kilitle

        Color c = fadeImage.color;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;

        endTextLabel.gameObject.SetActive(true);
        float delay = GameManager.Instance != null ? GameManager.Instance.letterDelay : 0.04f;
        yield return TypewriterEffect.Play(endTextLabel, endText, delay);
    }
}
