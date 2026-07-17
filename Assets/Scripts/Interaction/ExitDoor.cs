using UnityEngine;

// Çıkış kapısı: tıklanınca "diğer karakterle konuşmalısın" diyaloğunu oynatır.
// Kapı hiçbir koşulda açılmaz — oyun her zaman kötü sonla biter.
public class ExitDoor : MonoBehaviour, IClickable
{
    public void OnClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Exploring) return;
        gm.PlayDialogue(gm.doorDialogue, null);
    }
}
