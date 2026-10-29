using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// P4 scene wiring: drops the SkillTriggerPanel into Core.unity, hides the old
/// BaseWorkPanel it replaces, and adds a SkillFlowController to GameSystem with
/// every reference resolved by name.
///
/// Run with: unity command script.run --file AgentScripts/WireSkillPanel.cs --entry WireSkillPanel.Apply
/// </summary>
public static class WireSkillPanel
{
    const string SkillPrefabPath = "Assets/V2/Prefabs/UI/SkillTriggerPanel.prefab";

    public static string Apply()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "FAILED: Canvas not found - is Core.unity open?";

        var gameSystem = GameObject.Find("GameSystem");
        if (gameSystem == null) return "FAILED: GameSystem not found.";

        // 1. The new right-column trigger panel.
        Transform existing = canvas.transform.Find("SkillTriggerPanel");
        GameObject skillRoot;
        if (existing != null)
        {
            skillRoot = existing.gameObject;
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillPrefabPath);
            if (prefab == null) return $"FAILED: {SkillPrefabPath} not found.";

            skillRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            skillRoot.name = "SkillTriggerPanel";
        }
        skillRoot.SetActive(true);

        // 2. The dropdown panel this flow replaces. (Its name carries a trailing
        //    space in the scene, so match on the trimmed name.)
        Transform legacy = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name.Trim() == "BaseWorkPanel")
            {
                legacy = child;
                break;
            }
        }
        if (legacy != null) legacy.gameObject.SetActive(false);

        // 3. The controller that owns the flow.
        var flow = gameSystem.GetComponent<SkillFlowController>();
        if (flow == null) flow = gameSystem.AddComponent<SkillFlowController>();

        var serialized = new SerializedObject(flow);
        // The scene still carries the old deactivated CharCard roster, so pick
        // the roster that actually drives the hand rather than the first found.
        var hand = Object.FindAnyObjectByType<CharacterHandPanel>();
        var roster = hand != null ? hand.GetComponent<UnitRosterView>() : Object.FindAnyObjectByType<UnitRosterView>();
        serialized.FindProperty("rosterView").objectReferenceValue = roster;
        serialized.FindProperty("actionPanel").objectReferenceValue =
            Object.FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        serialized.FindProperty("skillPanel").objectReferenceValue =
            skillRoot.GetComponent<SkillTriggerPanel>();
        serialized.FindProperty("warningView").objectReferenceValue =
            canvas.GetComponentInChildren<WarningView>(true);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return $"OK\nskill: {skillRoot.name} active={skillRoot.activeSelf}\n" +
               $"legacy BaseWorkPanel active={(legacy != null ? legacy.gameObject.activeSelf.ToString() : "missing")}\n" +
               $"flow on: {gameSystem.name}";
    }
}
