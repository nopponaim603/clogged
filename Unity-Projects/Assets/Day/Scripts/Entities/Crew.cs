using System;
using System.Collections.Generic;
using UnityEngine;

public class Crew : MonoBehaviour
{
    public int Id;
    public string Name;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public float GatheringProficiency;
    public float SearchingProficiency;
    public float HuntingProficiency;
    public List<string> Perks = new List<string>();
    public int HireCost;
    public Vector2 Position;

    public bool IsBusy;

    // Reserved for a future permadeath mode. Currently always true —
    // crew never permanently die, they only go Down and recover.
    public bool IsAlive = true;

    public bool IsDown;

    [Range(0f, 1f)]
    [Tooltip("Fraction of MaxHp a downed crew must heal back up to before they're available again.")]
    public float DownRecoveryThreshold = 0.3f;

    private void Awake()
    {
        // Safety net for hand-authored crew prefabs: if MaxHp was left at 0
        // (easy to forget when setting up a new prefab by hand), treat the
        // starting Hp as the max instead of silently breaking Heal() forever.
        if (MaxHp <= 0f)
        {
            MaxHp = Hp;
        }
    }

    public float CalculateTravelTime(float distance)
    {
        return Mathf.Max(1f, distance / Mathf.Max(0.1f, Speed));
    }

    public float GetEffectiveGathering()
    {
        return GatheringProficiency + (HasPerk("gunslinger") ? 0.1f : 0f);
    }

    public float GetEffectiveSearching()
    {
        return SearchingProficiency;
    }

    public float GetEffectiveHunting()
    {
        return HuntingProficiency;
    }

    public bool HasPerk(string perk)
    {
        return Perks.Contains(perk);
    }

    /// <summary>
    /// Applies damage. Returns true if the crew was just knocked Down (Hp hit 0).
    /// There is no permanent death right now — a downed crew recovers via Heal().
    /// </summary>
    public bool TakeDamage(int damage)
    {
        Hp -= damage;
        if (Hp <= 0)
        {
            Hp = 0;
            IsDown = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Restores Hp, clamped to MaxHp. If the crew was Down and has healed past
    /// DownRecoveryThreshold, they come back up and are available again.
    /// </summary>
    public void Heal(float amount)
    {
        Hp = Mathf.Min(MaxHp, Hp + amount);

        if (IsDown && Hp >= MaxHp * DownRecoveryThreshold)
        {
            IsDown = false;
        }
    }
}
