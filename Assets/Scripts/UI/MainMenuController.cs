using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Ana menü: Play → fade in (karart) → menü kapanır → fade out (aç). Quit → oyundan çık.
// Sahne geçişi yoktur; menü aynı sahnede fullscreen panel olarak durur.
public class MainMenuController : MonoBehaviour
{
    public GameObject menuRoot;
    public Image menuFadeImage;
    public Button playButton;
    public Button quitButton;
    public float fadeDuration = 1f;

    [Tooltip("Menü arka planındaki firefly efektlerinin root'u; oyun başlayınca kapatılır")]
    public GameObject firefliesRoot;
    [Tooltip("Menü açıkken gizlenecek oyun UI'ları (örn. StatSliders) — Overlay canvas menünün üstüne çizildiği için")]
    public GameObject[] hideWhileMenuOpen;

    void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (menuFadeImage != null) menuFadeImage.gameObject.SetActive(false);
        SetHiddenUIActive(false);
    }

    void OnPlay()
    {
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        playButton.interactable = false;
        quitButton.interactable = false;

        menuFadeImage.gameObject.SetActive(true);
        yield return Fade(0f, 1f);

        // Ekran tamamen siyahken geçişi yap: menü + firefly kapanır, oyun HUD'ı açılır.
        menuRoot.SetActive(false);
        if (firefliesRoot != null) firefliesRoot.SetActive(false);
        SetHiddenUIActive(true);

        yield return Fade(1f, 0f);
        menuFadeImage.gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to)
    {
        Color c = menuFadeImage.color;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            menuFadeImage.color = c;
            yield return null;
        }
        c.a = to;
        menuFadeImage.color = c;
    }

    void SetHiddenUIActive(bool active)
    {
        if (hideWhileMenuOpen == null) return;
        foreach (var go in hideWhileMenuOpen)
            if (go != null) go.SetActive(active);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
