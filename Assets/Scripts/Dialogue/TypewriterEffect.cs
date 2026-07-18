using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Metni harf harf yazar. Skip yoktur — tüm harfler yazılana kadar bekler.
// onLetter callback'i her yazılan harf için çağrılır (blip sesi vb. için).
public static class TypewriterEffect
{
    public static IEnumerator Play(Text target, string content, float letterDelay,
        System.Action<char> onLetter = null)
    {
        target.text = "";
        if (string.IsNullOrEmpty(content)) yield break;

        var sb = new System.Text.StringBuilder(content.Length);
        foreach (char c in content)
        {
            sb.Append(c);
            target.text = sb.ToString();
            onLetter?.Invoke(c);
            yield return new WaitForSeconds(letterDelay);
        }
    }
}
