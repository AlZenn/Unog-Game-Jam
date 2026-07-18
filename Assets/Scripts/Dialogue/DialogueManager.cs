using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Diyalog panelini yönetir: satırları harf harf oynatır (blip sesli), cevap
// butonlarını gösterir, seçilen cevabın etkilerini uygular. Skip yoktur.
// Feel: panel alttan kayarak girer/çıkar, portre pop yapar, butonlar sırayla
// pop-in olur, cevap etkileri floating text ile gösterilir, olumsuz cevapta
// ekran sarsılır.
public class DialogueManager : MonoBehaviour
{
    [Header("UI Referansları")]
    public GameObject panelRoot;
    public Image portraitLeft;
    public Image portraitRight;
    public Text dialogueText;
    public GameObject answerRoot;
    public Button answerButtonA;
    public Button answerButtonB;
    public Text answerTextA;
    public Text answerTextB;

    [Header("Feel")]
    public float panelSlideDuration = 0.25f;
    public float buttonPopDuration = 0.2f;
    public float buttonPopStagger = 0.08f;
    public float portraitPopDuration = 0.15f;

    int chosenAnswer = -1;
    RectTransform panelRect;
    Vector2 panelShownPos;
    DialogueAudio audioFx;
    Sprite lastPortrait;
    bool lastSide;

    void Awake()
    {
        if (answerButtonA != null) answerButtonA.onClick.AddListener(() => chosenAnswer = 0);
        if (answerButtonB != null) answerButtonB.onClick.AddListener(() => chosenAnswer = 1);
        if (panelRoot != null)
        {
            panelRect = panelRoot.GetComponent<RectTransform>();
            if (panelRect != null) panelShownPos = panelRect.anchoredPosition;
            panelRoot.SetActive(false);
        }
        if (answerRoot != null) answerRoot.SetActive(false);
        audioFx = GetComponent<DialogueAudio>();
    }

    public void StartDialogue(DialogueData data, ClickableCharacter source)
    {
        StopAllCoroutines();
        StartCoroutine(RunDialogue(data, source));
    }

    IEnumerator RunDialogue(DialogueData data, ClickableCharacter source)
    {
        var gm = GameManager.Instance;
        panelRoot.SetActive(true);
        answerRoot.SetActive(false);
        lastPortrait = null;
        dialogueText.text = "";

        yield return SlidePanel(true);

        int letterIndex = 0;
        System.Action<char> onLetter = c =>
        {
            if (char.IsWhiteSpace(c)) return;
            if (letterIndex++ % 2 == 0 && audioFx != null) audioFx.PlayBlip();
        };

        foreach (var line in data.lines)
        {
            ShowPortrait(line);
            yield return TypewriterEffect.Play(dialogueText, line.text, gm.letterDelay, onLetter);
            // Oyuncu okuyabilsin diye bekleme (GameManager'dan ayarlanır).
            yield return new WaitForSeconds(gm.sentenceWaitTime);
        }

        if (!data.isNegative)
        {
            chosenAnswer = -1;
            answerTextA.text = data.answerA.answerText;
            answerTextB.text = data.answerB.answerText;
            answerRoot.SetActive(true);
            yield return PopButtons();
            while (chosenAnswer < 0) yield return null;
            answerRoot.SetActive(false);

            var answer = chosenAnswer == 0 ? data.answerA : data.answerB;
            gm.stats.ApplyEffects(answer.effects);
            SpawnStatTexts(answer);
            if (source != null) source.OnPositiveDialogueCompleted();
            gm.RegisterDialogueResult(true, true);
        }
        else
        {
            // Kapı diyaloğu source=null gelir: sayaca sayılmaz, shake de olmaz.
            gm.RegisterDialogueResult(false, source != null);
            if (source != null && CameraShake.Instance != null) CameraShake.Instance.Shake();
        }

        yield return SlidePanel(false);
        panelRoot.SetActive(false);
        gm.OnDialogueFinished();
    }

    // ---------------------------------------------------------------- feel

    IEnumerator SlidePanel(bool show)
    {
        if (panelRect == null) yield break;

        Vector2 hidden = panelShownPos + Vector2.down * (panelRect.rect.height + 120f);
        Vector2 from = show ? hidden : panelShownPos;
        Vector2 to = show ? panelShownPos : hidden;

        float t = 0f;
        while (t < panelSlideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / panelSlideDuration);
            p = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic
            panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, p);
            yield return null;
        }
        panelRect.anchoredPosition = to;
    }

    IEnumerator PopButtons()
    {
        Transform a = answerButtonA != null ? answerButtonA.transform : null;
        Transform b = answerButtonB != null ? answerButtonB.transform : null;
        if (a != null) a.localScale = Vector3.zero;
        if (b != null) b.localScale = Vector3.zero;

        if (a != null) StartCoroutine(PopScale(a));
        if (b != null)
        {
            yield return new WaitForSeconds(buttonPopStagger);
            yield return PopScale(b);
        }
    }

    // Ease-out-back: hafif taşmalı pop.
    IEnumerator PopScale(Transform target)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float t = 0f;
        while (t < buttonPopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / buttonPopDuration);
            float e = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
            target.localScale = Vector3.one * e;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    void ShowPortrait(DialogueLine line)
    {
        bool hasPortrait = line.portrait != null;
        portraitLeft.gameObject.SetActive(hasPortrait && line.isLeftSide);
        portraitRight.gameObject.SetActive(hasPortrait && !line.isLeftSide);
        if (!hasPortrait)
        {
            lastPortrait = null;
            return;
        }

        var img = line.isLeftSide ? portraitLeft : portraitRight;
        img.sprite = line.portrait;
        if (line.portrait != lastPortrait || line.isLeftSide != lastSide)
            StartCoroutine(PortraitPop(img));
        lastPortrait = line.portrait;
        lastSide = line.isLeftSide;
    }

    IEnumerator PortraitPop(Image img)
    {
        var tr = img.transform;
        Color c = img.color;

        float t = 0f;
        while (t < portraitPopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / portraitPopDuration);
            tr.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, p);
            c.a = p;
            img.color = c;
            yield return null;
        }
        tr.localScale = Vector3.one;
        c.a = 1f;
        img.color = c;
    }

    void SpawnStatTexts(DialogueAnswer answer)
    {
        if (answer.effects == null || answer.effects.Count == 0) return;

        var sliders = FindObjectsByType<DraggableStatSlider>(FindObjectsSortMode.None);
        foreach (var effect in answer.effects)
        {
            if (Mathf.Approximately(effect.amount, 0f)) continue;
            foreach (var slider in sliders)
            {
                if (slider.statType != effect.stat) continue;
                FloatingStatText.Spawn(slider, effect.amount);
                break;
            }
        }
    }
}
