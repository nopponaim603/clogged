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

    /// <summary>
    /// Randomizes every crew stat (Name, Hp/MaxHp, Speed, proficiencies, Perks)
    /// and assigns a fresh runtime Id + hire cost. Shared by both the plain
    /// random crew path and the random-appearance path below, so stats are
    /// generated the exact same way either way.
    /// </summary>
    private void RandomizeStats(Crew crew, int hireCost)
    {
        crew.Id = _nextId++;
        crew.Name = "Crew " + crew.Id;
        crew.Hp = Random.Range(80, 140);
        crew.MaxHp = crew.Hp;
        crew.Speed = Random.Range(1.2f, 2.4f);
        crew.GatheringProficiency = Random.Range(0.7f, 1.5f);
        crew.SearchingProficiency = Random.Range(0.7f, 1.5f);
        crew.HuntingProficiency = Random.Range(0.7f, 1.5f);
        crew.HireCost = hireCost;
        crew.Position = Vector2.zero;
        crew.Perks = new List<string> { "scout" };
    }

    /// <summary>
    /// Fully random crew with a plain, blank GameObject (no particular sprite).
    /// Used as a fallback if no appearance prefabs are configured.
    /// </summary>
    public Crew CreateRandomCrew(int hireCost)
    {
        var crew = new GameObject("Crew").AddComponent<Crew>();
        RandomizeStats(crew, hireCost);
        return crew;
    }

    /// <summary>
    /// Instantiates an appearance prefab (just for its sprite/visuals — any
    /// stats set on the prefab itself are ignored) and gives it a fresh set of
    /// randomized stats, same as CreateRandomCrew. Use this for a pool of
    /// "look" variants that should still come out with random stats.
    /// </summary>
    public Crew CreateRandomCrewFromAppearance(GameObject appearancePrefab, int hireCost)
    {
        if (appearancePrefab == null)
            return CreateRandomCrew(hireCost);

        GameObject instance = Instantiate(appearancePrefab);
        Crew crew = instance.GetComponent<Crew>();

        if (crew == null)
        {
            // Prefab was just a sprite/visual with no Crew component — add one.
            crew = instance.AddComponent<Crew>();
        }

        RandomizeStats(crew, hireCost);
        return crew;
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
