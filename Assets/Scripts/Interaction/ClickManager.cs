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

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Exploring) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // UI üzerine tıklanıyorsa (sliderlar vb.) dünyaya tıklama sayma.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector2 world = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        var hit = Physics2D.OverlapPoint(world);
        if (hit == null) return;

        var clickable = hit.GetComponentInParent<IClickable>();
        clickable?.OnClicked();
    }
}
