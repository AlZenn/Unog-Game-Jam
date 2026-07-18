using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public interface IClickable
{
    void OnClicked();
}

// New Input System ile sol tık algılar, 2D collider'a raycast yapar.
// Diyalog açıkken veya oyun bittiğinde devre dışıdır.
public class ClickManager : MonoBehaviour
{
    public Camera worldCamera;

    HoverHighlight currentHover;

    void Update()
    {
        var gm = GameManager.Instance;
        var mouse = Mouse.current;
        if (gm == null || gm.State != GameState.Exploring || mouse == null)
        {
            SetHover(null);
            return;
        }

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) { SetHover(null); return; }

        // UI üzerindeyken (sliderlar, menü vb.) dünya hover/tıklaması yok sayılır.
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        Collider2D hit = null;
        if (!overUI)
        {
            Vector2 world = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            hit = Physics2D.OverlapPoint(world);
        }

        SetHover(hit != null ? hit.GetComponentInParent<HoverHighlight>() : null);

        if (!overUI && hit != null && mouse.leftButton.wasPressedThisFrame)
            hit.GetComponentInParent<IClickable>()?.OnClicked();
    }

    void SetHover(HoverHighlight next)
    {
        if (currentHover == next) return;
        if (currentHover != null) currentHover.SetHovered(false);
        currentHover = next;
        if (currentHover != null) currentHover.SetHovered(true);
    }
}
