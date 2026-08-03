using UnityEngine;

public enum ResourceNodeType
{
    Wood,
    Stone,
    Iron,
    Food,
    Water,
    Circuit,
    Aluminum,
    Relic,
    Monster
}

public class ResourceNode : MonoBehaviour
{
    public ResourceNodeType Type;
    public int Amount;
    public int Difficulty;
    public bool IsRelic;
    public bool IsMonster;
    public string MonsterName = "";
    public Vector2 Position;
    public string Icon = "";

    public float GetActionTime(float crewSkill)
    {
        float baseTime = IsMonster ? 5f : 3f;
        return Mathf.Max(1f, baseTime - crewSkill * 0.5f);
    }
}
