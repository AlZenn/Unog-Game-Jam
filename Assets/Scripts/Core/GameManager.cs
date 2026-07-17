using System.Collections.Generic;
using UnityEngine;

public enum GameState { Exploring, InDialogue, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Diyalog zamanlaması")]
    [Tooltip("Harf harf yazma hızı (saniye/harf)")]
    public float letterDelay = 0.04f;
    [Tooltip("Cümle tamamen yazıldıktan sonra bekleme süresi (saniye)")]
    public float sentenceWaitTime = 1f;

    [Header("Referanslar")]
    public StatManager stats;
    public DialogueManager dialogueManager;
    public GameEndController endController;

    [Header("Karakterler")]
    public List<ClickableCharacter> characters = new List<ClickableCharacter>();

    [Header("Diyalog havuzları")]
    [Tooltip("Koşul sağlanmadığında rastgele seçilen olumsuz diyaloglar")]
    public List<DialogueData> negativeDialogues = new List<DialogueData>();
    public DialogueData doorDialogue;

    [Header("Kötü son")]
    [TextArea(2, 5)]
    public string endText = "Artık kimse seninle konuşmuyor.\nKapı hiç açılmadı.";

    public GameState State { get; private set; } = GameState.Exploring;
    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        Instance = this;
        if (stats == null) stats = GetComponent<StatManager>();
    }

    void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void PlayDialogue(DialogueData data, ClickableCharacter source)
    {
        if (State != GameState.Exploring || data == null || dialogueManager == null) return;
        SetState(GameState.InDialogue);
        dialogueManager.StartDialogue(data, source);
    }

    public DialogueData GetRandomNegativeDialogue()
    {
        if (negativeDialogues == null || negativeDialogues.Count == 0) return null;
        return negativeDialogues[Random.Range(0, negativeDialogues.Count)];
    }

    // DialogueManager diyalog kapanınca çağırır: point&click tekrar açılır, son kontrolü yapılır.
    public void OnDialogueFinished()
    {
        if (State == GameState.GameOver) return;
        SetState(GameState.Exploring);
        CheckGameEnd();
    }

    public void CheckGameEnd()
    {
        if (State == GameState.GameOver) return;
        if (CanAnyPositiveDialogueOpen()) return;

        SetState(GameState.GameOver);
        if (endController != null) endController.TriggerEnd(endText);
    }

    // Herhangi bir NPC'nin sıradaki olumlu diyaloğu, slider'ların kalan
    // aralıklarıyla hâlâ açılabilir durumda mı?
    public bool CanAnyPositiveDialogueOpen()
    {
        foreach (var character in characters)
        {
            if (character == null) continue;
            var next = character.NextPositiveDialogue;
            if (next != null && next.CanEverBeMet(stats)) return true;
        }
        return false;
    }
}
