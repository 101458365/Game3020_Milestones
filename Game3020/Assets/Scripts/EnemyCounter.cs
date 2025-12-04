using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EnemyCounter : MonoBehaviour
{
    public static EnemyCounter Instance;

    [Header("Enemy References")]
    public GameObject[] enemies;

    [Header("UI Settings")]
    public TextMeshProUGUI counterText;

    [Header("Scene Settings")]
    public string winSceneName = "WinScene";
    public float delayBeforeLoading = 1f;

    private bool winTriggered = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        int activeCount = CountActiveEnemies();
        UpdateUI(activeCount);
    }

    void Update()
    {
        // we only count enemies once per frame and use that result;
        int activeCount = CountActiveEnemies();
        UpdateUI(activeCount);
        CheckWinCondition(activeCount);
    }

    int CountActiveEnemies()
    {
        int count = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    void UpdateUI(int activeCount)
    {
        if (counterText != null)
        {
            counterText.text = "Enemies: " + activeCount;
        }
    }

    void CheckWinCondition(int activeCount)
    {
        if (winTriggered) return;

        if (activeCount == 0)
        {
            winTriggered = true;
            OnAllEnemiesDefeated();
        }
    }

    void OnAllEnemiesDefeated()
    {
        Debug.Log("All enemies defeated! Loading win scene...");
        enabled = false;
        Invoke(nameof(LoadWinScene), delayBeforeLoading);
    }

    void LoadWinScene()
    {
        SceneManager.LoadScene(winSceneName);
    }
}