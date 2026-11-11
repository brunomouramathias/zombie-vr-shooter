using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GazeButton : MonoBehaviour
{
    public float dwellTime = 1.5f;
    public float gazeAngle = 18f;
    public float maxGazeDistance = 5f;
    public Image progressRing;
    public UnityEvent onClick = new UnityEvent();

    private float dwellTimer;
    private bool isHovered;
    private bool pointerHover;
    private bool gazeHover;

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // Fallback de olhar baseado no ângulo da câmera. Funciona mesmo que o
        // collider do botão não seja atingido pelo raycast (comum em Canvas
        // World Space no Android/VR).
        UpdateGazeHover();
        isHovered = pointerHover || gazeHover;

        if (WasTriggerPressed() && isHovered)
        {
            TriggerClick();
            return;
        }

#if UNITY_EDITOR
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (isHovered && keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
        {
            TriggerClick();
            return;
        }
#endif

        if (isHovered)
        {
            dwellTimer += Time.deltaTime;
            SetProgress(dwellTimer / dwellTime);
            if (dwellTimer >= dwellTime) TriggerClick();
        }
        else
        {
            dwellTimer = Mathf.Max(0f, dwellTimer - Time.deltaTime * 3f);
            SetProgress(dwellTimer / dwellTime);
        }
    }

    private void UpdateGazeHover()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 to = transform.position - cam.transform.position;
        float distance = to.magnitude;
        if (distance > maxGazeDistance)
        {
            gazeHover = false;
            return;
        }

        float angle = Vector3.Angle(cam.transform.forward, to);
        gazeHover = angle <= gazeAngle;
    }

    public void OnPointerEnter() => pointerHover = true;
    public void OnPointerEnter(Transform player) => pointerHover = true;
    public void OnPointerExit() => pointerHover = false;
    public void OnPointerExit(Transform player) => pointerHover = false;

    public void OnPointerClick() => TriggerClick();

    public void TriggerClick()
    {
        isHovered = false;
        pointerHover = false;
        gazeHover = false;
        dwellTimer = 0f;
        SetProgress(0f);
        onClick?.Invoke();
    }

    private bool WasTriggerPressed()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

#if !UNITY_EDITOR
        // O trigger do Cardboard é um evento de um frame; checar todo frame
        // evita perder o clique por causa de throttle.
        return Google.XR.Cardboard.Api.IsTriggerPressed;
#else
        return false;
#endif
    }

    private void SetProgress(float value)
    {
        if (progressRing == null) return;
        progressRing.fillAmount = Mathf.Clamp01(value);
        progressRing.enabled = value > 0.01f;
    }
}
