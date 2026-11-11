using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Transform player;
    public int threshold = 30;
    public int maxAngle = 85;
    public float speed = 3f;
    public bool freezeY = true;
    public bool walkByLookingDown = false;
    public bool allowEditorMovement = false;
    public float raycastDistance = 30f;

    private GameObject currentHitObject;
    private PlayerWaypointMover mover;

    void Start()
    {
        if (player == null) player = transform.parent != null ? transform.parent : transform;
        mover = player.GetComponent<PlayerWaypointMover>();

        WeaponController weapon = GetComponent<WeaponController>();
        if (weapon == null) weapon = gameObject.AddComponent<WeaponController>();
        if (weapon.spawnPoint == null) weapon.spawnPoint = gameObject;

#if UNITY_EDITOR
        var trackedPose = GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (trackedPose != null && !UnityEngine.XR.XRSettings.isDeviceActive)
        {
            trackedPose.enabled = false;
        }
#endif
    }

    void Update()
    {
        HandleGazeRaycast();
        if (walkByLookingDown) HandleWalkLookingDown();
#if UNITY_EDITOR
        // Sempre permite olhar com o botão direito no editor; o deslocamento
        // continua controlado por allowEditorMovement.
        HandleEditorControls();
#endif
    }

    private void HandleGazeRaycast()
    {
        if (mover != null && mover.IsMoving)
        {
            SetHoveredObject(null);
            return;
        }

        // Jogador permanece parado: não ativa waypoints por olhar.
        // (Apenas GazeButton continua interativo.)

        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, raycastDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        GameObject hitObject = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.transform.IsChildOf(player)) continue;
            if (hit.collider.GetComponentInParent<GazeButton>() != null)
            {
                hitObject = hit.collider.gameObject;
                break;
            }
        }

        SetHoveredObject(hitObject);
    }

    private void SetHoveredObject(GameObject next)
    {
        if (currentHitObject == next) return;
        if (currentHitObject != null) SendPointerMessage(currentHitObject, "OnPointerExit");
        currentHitObject = next;
        if (currentHitObject != null) SendPointerMessage(currentHitObject, "OnPointerEnter");
    }

    private void SendPointerMessage(GameObject target, string method)
    {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is WaypointController || behaviours[i] is GazeButton)
            {
                behaviours[i].SendMessage(method, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void HandleWalkLookingDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        float pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        if (pitch < threshold || pitch > maxAngle || player == null) return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return;
        PlayerCollision.Move(player, forward.normalized * (speed * Time.deltaTime));
        if (freezeY) player.position = new Vector3(player.position.x, 3f, player.position.z);
    }

#if UNITY_EDITOR
    private void HandleEditorControls()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            transform.localRotation *= Quaternion.Euler(-delta.y * 0.2f, delta.x * 0.2f, 0f);
        }

        if (!allowEditorMovement) return;
        if (keyboard == null || player == null || (GameManager.Instance != null && !GameManager.Instance.IsPlaying)) return;

        Vector3 direction = Vector3.zero;
        if (keyboard.wKey.isPressed) direction += transform.forward;
        if (keyboard.sKey.isPressed) direction -= transform.forward;
        if (keyboard.dKey.isPressed) direction += transform.right;
        if (keyboard.aKey.isPressed) direction -= transform.right;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            PlayerCollision.Move(player, direction.normalized * (speed * Time.deltaTime));
        }
    }
#endif
}
