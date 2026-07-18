using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Butona eklenir: üzerine gelince hover sesi, tıklayınca click sesi çalar.
// Sesler UISoundManager'dan gelir (Managers objesinde, tek yerden atanır).
public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public bool playHover = true;
    public bool playClick = true;

    Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    bool Interactable => selectable == null || selectable.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHover && Interactable && UISoundManager.Instance != null)
            UISoundManager.Instance.PlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClick && Interactable && UISoundManager.Instance != null)
            UISoundManager.Instance.PlayClick();
    }
}
