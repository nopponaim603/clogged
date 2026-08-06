using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain-data preview of a crew's stats — NOT a GameObject/MonoBehaviour.
/// Used by CrewIntakeUI to show stat cards without instantiating anything in
/// the scene. Only once the player actually selects and confirms a card does
/// CrewManager.InstantiateFromData() turn this into a real Crew GameObject.
/// </summary>
[Serializable]
public class CrewData
{
    /// <summary>Id reserved at preview time — reused as the real Crew.Id if instantiated, so no gaps/mismatches.</summary>
    public int PreviewId;
    public string Name;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public float GatheringProficiency;
    public float SearchingProficiency;
    public float HuntingProficiency;
    public List<string> Perks = new List<string>();
    public int HireCost;

    /// <summary>Visual variant to instantiate from, if picked. Null = plain blank GameObject.</summary>
    public GameObject AppearancePrefab;
}
