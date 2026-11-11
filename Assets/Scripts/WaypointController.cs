using UnityEngine;
using UnityEngine.UI;

public class WaypointController : MonoBehaviour
{
    public float dwellTime = 1.5f;
    public Image progressRing;
    public MeshRenderer markerRenderer;

    private float dwellTimer;
    private bool isHovered;
    private bool isVisible = true;
    private Material markerMaterial;
    private static float nextTouchCheck;

    void Start()
    {
        if (PlayerWaypointMover.Instance != null) PlayerWaypointMover.Instance.RegisterWaypoint(this);
        if (markerRenderer != null) markerMaterial = markerRenderer.material;
        SetProgress(0f);
    }

    void Update()
    {
        if (!isVisible) return;

        if (markerMaterial != null)
        {
            float pulse = 0.8f + 0.2f * Mathf.PingPong(Time.time * 1.5f, 1f);
            markerMaterial.color = new Color(0.2f * pulse, 0.8f * pulse, pulse, 1f);
        }

        if (!isHovered)
        {
            dwellTimer = Mathf.Max(0f, dwellTimer - Time.deltaTime * 2f);
            SetProgress(dwellTimer / dwellTime);
            return;
        }

        if (WasTriggerPressed())
        {
            TriggerMovement();
            return;
        }

        dwellTimer += Time.deltaTime;
        SetProgress(dwellTimer / dwellTime);
        if (dwellTimer >= dwellTime) TriggerMovement();
    }

    private bool WasTriggerPressed()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

#if !UNITY_EDITOR
        if (Time.unscaledTime >= nextTouchCheck)
        {
            nextTouchCheck = Time.unscaledTime + 0.05f;
            return Google.XR.Cardboard.Api.IsTriggerPressed;
        }
#endif
        return false;
    }

    public void OnPointerEnter() => SetHovered(true);
    public void OnPointerExit() => SetHovered(false);
    public void OnPointerEnter(Transform player) => SetHovered(true);
    public void OnPointerExit(Transform player) => SetHovered(false);

    private void SetHovered(bool hovered)
    {
        if (!isVisible) return;
        if (hovered && PlayerWaypointMover.Instance != null && PlayerWaypointMover.Instance.IsMoving) return;
        isHovered = hovered;
    }

    private void TriggerMovement()
    {
        isHovered = false;
        dwellTimer = 0f;
        SetProgress(0f);
        if (PlayerWaypointMover.Instance != null) PlayerWaypointMover.Instance.MoveToWaypoint(this);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        isHovered = false;
        dwellTimer = 0f;
        SetProgress(0f);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
        if (markerRenderer != null) markerRenderer.enabled = visible;
        Transform canvas = transform.Find("WaypointCanvas");
        if (canvas != null) canvas.gameObject.SetActive(visible);
    }

    private void SetProgress(float value)
    {
        if (progressRing == null) return;
        progressRing.fillAmount = Mathf.Clamp01(value);
        progressRing.enabled = value > 0.01f;
    }
}
