using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public Dictionary<string, int> Resources = new Dictionary<string, int>
    {
        ["wood"] = 0,
        ["food"] = 20,
        ["relic"] = 0
    };

    public Dictionary<string, int> DayResources = new Dictionary<string, int>();
    public Dictionary<string, int> MonsterParts = new Dictionary<string, int>
    {
        ["fangs"] = 0
    };

    public float BaseHp = 100f;
    public float MaxBaseHp = 100f;

    public void AddResource(string type, int amount)
    {
        if (!Resources.ContainsKey(type))
            return;

        Resources[type] += amount;

        if (DayResources.ContainsKey(type))
            DayResources[type] += amount;
        else
            DayResources[type] = amount;
    }

    public int GetResource(string type)
    {
        return Resources.ContainsKey(type) ? Resources[type] : 0;
    }

    public void AddMonsterPart(string type, int amount)
    {
        if (!MonsterParts.ContainsKey(type))
            return;

        MonsterParts[type] += amount;
    }

    public Dictionary<string, int> LoseDayResources()
    {
        var lost = new Dictionary<string, int>();

        foreach (var kvp in DayResources)
        {
            if (kvp.Value <= 0) continue;

            int loss = Mathf.FloorToInt(kvp.Value / 2f);
            if (loss <= 0) loss = 1;

            Resources[kvp.Key] -= loss;
            lost[kvp.Key] = loss;
        }

        DayResources.Clear();
        return lost;
    }

    public bool ConsumeFood(int crewCount)
    {
        int needed = crewCount * 2;
        if (Resources["food"] >= needed)
        {
            Resources["food"] -= needed;
            return true;
        }

        Resources["food"] = 0;
        return false;
    }

    public bool TakeBaseDamage(float damage)
    {
        BaseHp -= damage;
        return BaseHp <= 0f;
    }
}