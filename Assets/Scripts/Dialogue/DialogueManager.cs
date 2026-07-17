using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Diyalog panelini yönetir: satırları harf harf oynatır, cevap butonlarını gösterir,
// seçilen cevabın etkilerini uygular. Skip yoktur.
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

    int chosenAnswer = -1;

    void Awake()
    {
        if (answerButtonA != null) answerButtonA.onClick.AddListener(() => chosenAnswer = 0);
        if (answerButtonB != null) answerButtonB.onClick.AddListener(() => chosenAnswer = 1);
        if (panelRoot != null) panelRoot.SetActive(false);
        if (answerRoot != null) answerRoot.SetActive(false);
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

        foreach (var line in data.lines)
        {
            ShowPortrait(line);
            yield return TypewriterEffect.Play(dialogueText, line.text, gm.letterDelay);
            // Oyuncu okuyabilsin diye bekleme (GameManager'dan ayarlanır).
            yield return new WaitForSeconds(gm.sentenceWaitTime);
        }

        if (!data.isNegative)
        {
            chosenAnswer = -1;
            answerTextA.text = data.answerA.answerText;
            answerTextB.text = data.answerB.answerText;
            answerRoot.SetActive(true);
            while (chosenAnswer < 0) yield return null;
            answerRoot.SetActive(false);

            var answer = chosenAnswer == 0 ? data.answerA : data.answerB;
            gm.stats.ApplyEffects(answer.effects);
            if (source != null) source.OnPositiveDialogueCompleted();
        }

        panelRoot.SetActive(false);
        gm.OnDialogueFinished();
    }

    void ShowPortrait(DialogueLine line)
    {
        bool hasPortrait = line.portrait != null;
        portraitLeft.gameObject.SetActive(hasPortrait && line.isLeftSide);
        portraitRight.gameObject.SetActive(hasPortrait && !line.isLeftSide);
        if (hasPortrait)
            (line.isLeftSide ? portraitLeft : portraitRight).sprite = line.portrait;
    }
}
