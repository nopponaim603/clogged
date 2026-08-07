using UnityEngine;
using UnityEngine.SceneManagement;

public class NightGameManager : MonoBehaviour
{
    public static NightGameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("ยังไม่ได้ใส่ GameOverPanel ใน NightGameManager");
        }

        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}