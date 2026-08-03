using UnityEngine;

/// <summary>
/// Re-wires scene-local references into the persistent GameManager singleton
/// every time the ShipDay scene loads, then explicitly kicks off day-start
/// logic. Uses Start() (not Awake()) because Unity does not guarantee Awake()
/// order between different GameObjects — Start() is guaranteed to run only
/// after every object's Awake() in the scene has already completed, so
/// GameManager.Instance is safely set by then.
///
/// Day-start logic (healing crew, crew intake, GenerateWorld) is triggered
/// explicitly here — AFTER the references are wired — rather than from
/// SceneManager.sceneLoaded, because that event fires before this Start()
/// runs and would use a still-null BaseTransform.
/// </summary>
public class ShipDaySceneBootstrap : MonoBehaviour
{
    public Transform BaseTransform;
    public GameObject NodeWoodPrefab;
    public GameObject NodeFoodPrefab;
    public GameObject NodeRelicPrefab;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("ShipDaySceneBootstrap: GameManager.Instance is still null in Start(). " +
                            "Make sure a GameManager exists in the scene or persisted from a previous one.");
            return;
        }

        GameManager.Instance.BaseTransform = BaseTransform;
        GameManager.Instance.NodeWoodPrefab = NodeWoodPrefab;
        GameManager.Instance.NodeFoodPrefab = NodeFoodPrefab;
        GameManager.Instance.NodeRelicPrefab = NodeRelicPrefab;

        GameManager.Instance.InitializeShipDayScene();
    }
}
