using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Single-file autosave, written every time the player ends a day.
///
/// The save is taken from the live singletons rather than pushed to by them, so
/// there is exactly one place that knows the file layout. Loading is two-step:
/// <see cref="QueueLoad"/> stashes the file and the game scene's
/// <see cref="RunBootstrap"/> applies it after the scene has woken up.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "clogged-run.json";

    private static SaveData pendingLoad;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave
    {
        get
        {
            try { return File.Exists(FilePath); }
            catch (Exception e) { Debug.LogWarning($"[SaveSystem] {e.Message}"); return false; }
        }
    }

    /// <summary>True while a load is waiting for the game scene to come up.</summary>
    public static bool HasPendingLoad => pendingLoad != null;

    /// <summary>Writes the current run to disk. Safe to call when a singleton is missing (writes what it has).</summary>
    public static bool SaveNow()
    {
        var data = Capture();
        if (data == null) return false;

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Could not write {FilePath}: {e.Message}");
            return false;
        }
    }

    /// <summary>Reads the file and parks it for <see cref="RunBootstrap"/>. False when there is nothing usable to load.</summary>
    public static bool QueueLoad()
    {
        var data = Read();
        if (data == null) return false;

        pendingLoad = data;
        return true;
    }

    /// <summary>Hands the parked save to the game scene, once.</summary>
    public static SaveData ConsumePendingLoad()
    {
        var data = pendingLoad;
        pendingLoad = null;
        return data;
    }

    public static void ClearPendingLoad() => pendingLoad = null;

    /// <summary>Reads the save without queueing it - for a menu that only wants to show where the run is.</summary>
    public static SaveData Peek() => Read();

    public static void Delete()
    {
        pendingLoad = null;

        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Could not delete {FilePath}: {e.Message}");
        }
    }

    private static SaveData Capture()
    {
        var data = new SaveData
        {
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            stats = RunStats.TakeSnapshot()
        };

        var days = DayManager.Instance;
        if (days != null)
        {
            data.currentDay = days.CurrentDay;
            data.deadlineDay = days.DeadlineDay;
        }

        var res = RunResources.Instance;
        if (res != null)
        {
            data.food = res.Food;
            data.cookingMaterial = res.CookingMaterial;
            data.medicine = res.Medicine;
            data.materials = res.Materials;
            data.coreItemA = res.HasCoreItemA;
            data.coreItemB = res.HasCoreItemB;
            data.variant = (int)res.Variant;
        }

        var units = UnitManager.Instance;
        if (units != null)
        {
            foreach (var unit in units.Units)
            {
                data.units.Add(new SaveData.UnitSnapshot
                {
                    unitName = unit.unitName,
                    state = (int)unit.state,
                    currentActionPoints = unit.currentActionPoints,
                    starvationStacks = unit.starvationStacks
                });
            }
        }

        return data;
    }

    private static SaveData Read()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;

            var json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[SaveSystem] Save file is empty or unreadable.");
                return null;
            }

            if (data.version != SaveData.CurrentVersion)
            {
                Debug.LogWarning($"[SaveSystem] Save version {data.version} does not match {SaveData.CurrentVersion}; ignoring it.");
                return null;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Could not read {FilePath}: {e.Message}");
            return null;
        }
    }
}
