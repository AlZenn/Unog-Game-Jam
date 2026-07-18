using System.Collections.Generic;
using UnityEngine;

// Point & click NPC. Tıklanınca: sıradaki olumlu diyaloğun koşulları mevcut slider
// değerleriyle sağlanıyorsa onu, sağlanmıyorsa havuzdan rastgele olumsuz diyalog oynatır.
public class ClickableCharacter : MonoBehaviour, IClickable
{
    [Header("Kimlik")]
    public string characterName;
    [Tooltip("Portre çözümü için karakter kimliği (olumsuz diyaloglarda bu karakterin portresi gösterilir)")]
    public SpeakerCharacter characterId;
    public StatType specialStat;

    [Header("Özel değer (her olumlu diyalogda azalır)")]
    public float specialValue = 100f;
    public float specialDecreasePerDialogue = 10f;

    [Header("Olumlu diyaloglar (sıralı oynar)")]
    public List<DialogueData> positiveDialogues = new List<DialogueData>();
    public int nextDialogueIndex;

    public DialogueData NextPositiveDialogue =>
        nextDialogueIndex < positiveDialogues.Count ? positiveDialogues[nextDialogueIndex] : null;

    public void OnClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Exploring) return;

        var hover = GetComponent<HoverHighlight>();
        if (hover != null) hover.TriggerClickPunch();

        var next = NextPositiveDialogue;
        if (next != null && next.MeetsRequirements(gm.stats))
            gm.PlayDialogue(next, this);
        else
            // source geçilir ki olumsuz cevap art arda sayacına sayılsın (kapı sayılmaz).
            gm.PlayDialogue(gm.GetRandomNegativeDialogue(), this);
    }

    // Olumlu diyalog bitip cevap seçildiğinde DialogueManager çağırır.
    public void OnPositiveDialogueCompleted()
    {
        specialValue = Mathf.Max(0f, specialValue - specialDecreasePerDialogue);
        nextDialogueIndex++;
    }
}
