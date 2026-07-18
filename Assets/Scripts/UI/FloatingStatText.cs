using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Cevap seçilince slider yanında beliren "+10" / "-5" yazısı:
// yukarı süzülür, solarak kaybolur, kendini yok eder.
public class FloatingStatText : MonoBehaviour
{
    public static void Spawn(DraggableStatSlider slider, float amount)
    {
        if (slider == null) return;
        var canvas = slider.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("FloatingStatText", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.position = slider.transform.position;
        rt.anchoredPosition += new Vector2(70f, 40f);
        rt.sizeDelta = new Vector2(160f, 44f);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = (amount > 0f ? "+" : "") + Mathf.RoundToInt(amount);
        text.color = amount > 0f
            ? new Color(0.45f, 1f, 0.45f)
            : new Color(1f, 0.4f, 0.4f);

        var runner = go.AddComponent<FloatingStatText>();
        runner.StartCoroutine(runner.FloatRoutine(rt, text));
    }

    IEnumerator FloatRoutine(RectTransform rt, Text text)
    {
        const float duration = 1f;
        const float rise = 70f;
        Vector2 start = rt.anchoredPosition;
        Color c = text.color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = start + new Vector2(0f, rise * (1f - Mathf.Pow(1f - p, 2f)));
            c.a = 1f - p * p;
            text.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
