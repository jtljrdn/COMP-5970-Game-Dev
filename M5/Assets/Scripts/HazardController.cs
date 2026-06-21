using UnityEngine;

public class Hazard : MonoBehaviour
{
    public enum HazardType { Slow, Knockback, ScorePenalty }
    public HazardType type;

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        if (!other.gameObject.TryGetComponent<PlayerMovement>(out var player)) return;

        switch (type)
        {
            case HazardType.Slow: player.ApplySlow(0.5f, 1.5f); break;   // 50% speed for 1.5s
            case HazardType.Knockback: player.ApplyKnockback(20f); break;
            case HazardType.ScorePenalty: player.PenalizeScore(5f); break;   // lose 5s of score
        }
    }
}