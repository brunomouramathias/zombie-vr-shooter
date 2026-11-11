using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerWaypointMover : MonoBehaviour
{
    public static PlayerWaypointMover Instance { get; private set; }
    public static event System.Action<Vector3> OnPlayerMoved;

    public float moveSpeed = 3f;
    public bool movementEnabled = false;
    public bool IsMoving { get; private set; }

    private WaypointController currentWaypoint;
    private readonly List<WaypointController> allWaypoints = new List<WaypointController>();

    void Awake()
    {
        Instance = this;
        OnPlayerMoved = null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (!movementEnabled) HideAllWaypoints();
    }

    public void RegisterWaypoint(WaypointController waypoint)
    {
        if (waypoint != null && !allWaypoints.Contains(waypoint))
        {
            allWaypoints.Add(waypoint);
            if (!movementEnabled) waypoint.SetVisible(false);
        }
    }

    private void HideAllWaypoints()
    {
        for (int i = 0; i < allWaypoints.Count; i++)
        {
            if (allWaypoints[i] != null) allWaypoints[i].SetVisible(false);
        }
    }

    public void MoveToWaypoint(WaypointController waypoint)
    {
        if (!movementEnabled) return;
        if (IsMoving || waypoint == null || waypoint == currentWaypoint) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (NavMesh.SamplePosition(waypoint.transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            StartCoroutine(SlideTo(hit.position, waypoint));
        }
    }

    private IEnumerator SlideTo(Vector3 destination, WaypointController waypoint)
    {
        IsMoving = true;
        Vector3 startPosition = transform.position;
        float eyeHeight = startPosition.y;

        NavMeshHit startHit;
        if (!NavMesh.SamplePosition(startPosition, out startHit, 4f, NavMesh.AllAreas))
        {
            IsMoving = false;
            yield break;
        }

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, destination, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
        {
            IsMoving = false;
            yield break;
        }

        bool blocked = false;
        float globalDeadline = Time.time + 45f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 corner = path.corners[i];
            corner.y = eyeHeight;
            float cornerDeadline = Time.time + Mathf.Max(1.5f, Vector3.Distance(transform.position, corner) / Mathf.Max(0.1f, moveSpeed) * 2f);
            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(corner.x, 0, corner.z)) > 0.04f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, corner, moveSpeed * Time.deltaTime);
                Vector3 step = next - transform.position;
                step.y = 0f;
                PlayerCollision.Move(transform, step);
                transform.position = new Vector3(transform.position.x, eyeHeight, transform.position.z);

                if (Time.time > cornerDeadline || Time.time > globalDeadline)
                {
                    blocked = true;
                    break;
                }
                yield return null;
            }
            if (blocked) break;
        }

        if (blocked) Debug.LogWarning("[PlayerWaypointMover] Caminho bloqueado; movimento interrompido no ponto seguro atual.");

        Vector3 finalStep = new Vector3(destination.x - transform.position.x, 0f, destination.z - transform.position.z);
        PlayerCollision.Move(transform, finalStep);
        transform.position = new Vector3(transform.position.x, eyeHeight, transform.position.z);

        if (!blocked) currentWaypoint = waypoint;
        IsMoving = false;
        UpdateWaypointVisibilities();
        OnPlayerMoved?.Invoke(transform.position);

        ZombieController[] enemies = Object.FindObjectsByType<ZombieController>();
        foreach (ZombieController enemy in enemies)
        {
            if (enemy != null) enemy.UpdateDestination();
        }
    }

    public void UpdateWaypointVisibilities()
    {
        for (int i = allWaypoints.Count - 1; i >= 0; i--)
        {
            WaypointController waypoint = allWaypoints[i];
            if (waypoint == null)
            {
                allWaypoints.RemoveAt(i);
                continue;
            }
            Vector3 difference = waypoint.transform.position - transform.position;
            difference.y = 0f;
            bool isCurrent = waypoint == currentWaypoint || difference.magnitude < 1.2f;
            waypoint.SetVisible(!isCurrent);
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!movementEnabled) return;
        if (IsMoving || allWaypoints.Count == 0) return;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;
        UnityEngine.InputSystem.Controls.KeyControl[] keys =
        {
            keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key,
            keyboard.digit5Key, keyboard.digit6Key, keyboard.digit7Key, keyboard.digit8Key
        };
        for (int i = 0; i < Mathf.Min(keys.Length, allWaypoints.Count); i++)
        {
            if (keys[i].wasPressedThisFrame && allWaypoints[i] != null)
            {
                MoveToWaypoint(allWaypoints[i]);
                return;
            }
        }
    }
#endif
}
