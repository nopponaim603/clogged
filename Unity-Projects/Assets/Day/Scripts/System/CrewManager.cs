using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrewManager : MonoBehaviour
{
    public List<Crew> Crews = new List<Crew>();
    public List<Crew> AvailableCrews = new List<Crew>();
    public List<Crew> BusyCrews = new List<Crew>();
    public List<Crew> DownCrews = new List<Crew>();

    private int _nextId = 1;

    // ------------------------------------------------------------------
    // Preview-only stat generation (no GameObject created here)
    // ------------------------------------------------------------------

    /// <summary>
    /// Generates a random crew's stats as plain data — no GameObject is
    /// created. Reserves an Id up front so that if this data later gets
    /// instantiated (see InstantiateFromData), the Id and displayed "Crew N"
    /// name line up with no gaps. If it's never instantiated (not picked),
    /// the Id is simply never used — harmless.
    /// </summary>
    public CrewData GenerateRandomCrewData(GameObject appearancePrefab, int hireCost)
    {
        int id = _nextId++;
        float hp = Random.Range(80, 140);

        return new CrewData
        {
            PreviewId = id,
            Name = "Crew " + id,
            Hp = hp,
            MaxHp = hp,
            Speed = Random.Range(1.2f, 2.4f),
            GatheringProficiency = Random.Range(0.7f, 1.5f),
            SearchingProficiency = Random.Range(0.7f, 1.5f),
            HuntingProficiency = Random.Range(0.7f, 1.5f),
            Perks = new List<string> { "scout" },
            HireCost = hireCost,
            AppearancePrefab = appearancePrefab
        };
    }

    /// <summary>
    /// Turns previewed CrewData into an actual Crew GameObject in the scene —
    /// call this ONLY once the player has actually picked this crew (e.g. on
    /// intake Confirm). Nothing is instantiated for data that's never passed
    /// here, so unpicked offers never touch the scene at all.
    /// </summary>
    public Crew InstantiateFromData(CrewData data)
    {
        if (data == null) return null;

        GameObject instance = data.AppearancePrefab != null
            ? Instantiate(data.AppearancePrefab)
            : new GameObject("Crew");

        Crew crew = instance.GetComponent<Crew>();
        if (crew == null)
            crew = instance.AddComponent<Crew>();

        crew.Id = data.PreviewId;
        crew.Name = data.Name;
        crew.Hp = data.Hp;
        crew.MaxHp = data.MaxHp;
        crew.Speed = data.Speed;
        crew.GatheringProficiency = data.GatheringProficiency;
        crew.SearchingProficiency = data.SearchingProficiency;
        crew.HuntingProficiency = data.HuntingProficiency;
        crew.Perks = new List<string>(data.Perks);
        crew.HireCost = data.HireCost;
        crew.Position = Vector2.zero;

        return crew;
    }

    // ------------------------------------------------------------------
    // Immediate-instantiation helpers (kept for other callers/uses)
    // ------------------------------------------------------------------

    /// <summary>
    /// Fully random crew with a plain, blank GameObject (no particular sprite),
    /// instantiated immediately. Prefer GenerateRandomCrewData() + 
    /// InstantiateFromData() when you need a preview-before-commit flow.
    /// </summary>
    public Crew CreateRandomCrew(int hireCost)
    {
        CrewData data = GenerateRandomCrewData(null, hireCost);
        return InstantiateFromData(data);
    }

    /// <summary>
    /// Instantiates an appearance prefab immediately with fresh randomized
    /// stats. Prefer GenerateRandomCrewData() + InstantiateFromData() when you
    /// need a preview-before-commit flow.
    /// </summary>
    public Crew CreateRandomCrewFromAppearance(GameObject appearancePrefab, int hireCost)
    {
        CrewData data = GenerateRandomCrewData(appearancePrefab, hireCost);
        return InstantiateFromData(data);
    }

    /// <summary>
    /// Instantiates a fully hand-authored Crew prefab and KEEPS its authored
    /// stats (Name, Hp, MaxHp, Speed, proficiencies, Perks) — only Id and
    /// HireCost are assigned fresh. Use this if you want specific named crew
    /// with fixed stats instead of randomized ones.
    /// </summary>
    public Crew InstantiateCrewFromPrefab(GameObject crewPrefab, int hireCost)
    {
        if (crewPrefab == null) return null;

        GameObject instance = Instantiate(crewPrefab);
        Crew crew = instance.GetComponent<Crew>();

        if (crew == null)
        {
            Debug.LogWarning("Crew prefab '" + crewPrefab.name + "' has no Crew component attached.");
            Destroy(instance);
            return null;
        }

        crew.Id = _nextId++;
        crew.HireCost = hireCost;
        crew.Position = Vector2.zero;

        return crew;
    }

    // ------------------------------------------------------------------
    // Hiring / mission state (unchanged)
    // ------------------------------------------------------------------

    public bool HireCrew(Crew crew, int points)
    {
        if (points < crew.HireCost) return false;
        Crews.Add(crew);
        AvailableCrews.Add(crew);
        return true;
    }

    /// <summary>
    /// Hires a crew with no cost check. Used for the one-time free intake at
    /// the start of a run, where the offered crew are meant to be free picks.
    /// </summary>
    public bool HireCrewFree(Crew crew)
    {
        if (crew == null) return false;
        if (Crews.Contains(crew)) return false;

        Crews.Add(crew);
        AvailableCrews.Add(crew);
        return true;
    }

    public bool AssignMission(int crewId)
    {
        Crew crew = Crews.FirstOrDefault(c => c.Id == crewId);
        if (crew == null || !crew.IsAlive) return false;
        if (crew.IsDown) return false;
        if (BusyCrews.Contains(crew)) return false;

        crew.IsBusy = true;
        AvailableCrews.Remove(crew);
        BusyCrews.Add(crew);
        return true;
    }

    public void CompleteMission(int crewId)
    {
        Crew crew = Crews.FirstOrDefault(c => c.Id == crewId);
        if (crew == null) return;

        crew.IsBusy = false;
        BusyCrews.Remove(crew);

        if (crew.IsDown)
        {
            if (!DownCrews.Contains(crew))
                DownCrews.Add(crew);
            return;
        }

        if (crew.IsAlive)
        {
            AvailableCrews.Add(crew);
        }
    }

    /// <summary>
    /// Pulls a crew straight into the Down bucket (used when a mission is
    /// aborted early because the crew was knocked out mid-mission).
    /// </summary>
    public void MarkCrewDown(int crewId)
    {
        Crew crew = Crews.FirstOrDefault(c => c.Id == crewId);
        if (crew == null) return;

        crew.IsBusy = false;
        BusyCrews.Remove(crew);
        AvailableCrews.Remove(crew);

        if (!DownCrews.Contains(crew))
            DownCrews.Add(crew);
    }

    /// <summary>
    /// Heals every Down crew by amount. Anyone who recovers past their
    /// DownRecoveryThreshold moves back into AvailableCrews automatically.
    /// Call this once per day-start (or on any "rest at base" tick).
    /// </summary>
    public void HealDownCrews(float amount)
    {
        for (int i = DownCrews.Count - 1; i >= 0; i--)
        {
            Crew crew = DownCrews[i];
            crew.Heal(amount);

            if (!crew.IsDown)
            {
                DownCrews.RemoveAt(i);
                if (!AvailableCrews.Contains(crew))
                    AvailableCrews.Add(crew);
            }
        }
    }
}
