using UnityEngine;

public static class PlayerCollision
{
    private const float Skin = 0.03f;

    // Move o jogador com uma cápsula e desliza pelas paredes em vez de atravessá-las.
    public static void Move(Transform player, Vector3 delta)
    {
        if (player == null || delta.sqrMagnitude < 0.000001f) return;

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        float radius = capsule != null ? capsule.radius * Mathf.Max(player.lossyScale.x, player.lossyScale.z) : 0.35f;
        float height = capsule != null ? capsule.height * player.lossyScale.y : 1.8f;
        Vector3 center = capsule != null ? player.TransformPoint(capsule.center) : player.position + Vector3.up * 0.9f;
        float half = Mathf.Max(radius, height * 0.5f);
        Vector3 bottom = center - Vector3.up * (half - radius);
        Vector3 top = center + Vector3.up * (half - radius);

        Vector3 remaining = delta;
        for (int i = 0; i < 3; i++)
        {
            float distance = remaining.magnitude;
            if (distance <= 0.0001f) break;
            Vector3 direction = remaining / distance;

            if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit, distance + Skin, ~0, QueryTriggerInteraction.Ignore)
                && !IsPlayerCollider(hit.collider, player))
            {
                float allowed = Mathf.Max(0f, hit.distance - Skin);
                if (allowed > 0f)
                {
                    player.position += direction * allowed;
                    bottom += direction * allowed;
                    top += direction * allowed;
                }

                remaining = Vector3.ProjectOnPlane(remaining, hit.normal);
            }
            else
            {
                player.position += remaining;
                break;
            }
        }

        Depenetrate(player, capsule, bottom, top, radius);
    }

    private static bool IsPlayerCollider(Collider collider, Transform player)
    {
        return collider == null || collider.transform == player || collider.transform.IsChildOf(player);
    }

    private static void Depenetrate(Transform player, CapsuleCollider capsule, Vector3 bottom, Vector3 top, float radius)
    {
        if (capsule == null) return;

        Collider[] overlaps = new Collider[8];
        for (int pass = 0; pass < 3; pass++)
        {
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            bool pushed = false;
            for (int i = 0; i < count; i++)
            {
                Collider other = overlaps[i];
                if (IsPlayerCollider(other, player)) continue;
                if (Physics.ComputePenetration(
                        capsule, capsule.transform.position, capsule.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    Vector3 push = direction * (distance + Skin);
                    player.position += push;
                    bottom += push;
                    top += push;
                    pushed = true;
                }
            }
            if (!pushed) return;
        }
    }
}
