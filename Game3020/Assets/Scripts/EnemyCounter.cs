using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EnemyCounter : MonoBehaviour
{
    public static EnemyCounter Instance;

    [Header("Enemy References")]
    public GameObject enemy1;
    public GameObject enemy2;

    [Header("UI Settings")]
    public TextMeshProUGUI counterText;

    [Header("Scene Settings")]
    public string winSceneName = "WinScene";
    public float delayBeforeLoading = 1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
        CheckWinCondition();
    }

    void UpdateUI()
    {
        int activeCount = 0;
        if (enemy1 != null && enemy1.activeSelf) activeCount++;
        if (enemy2 != null && enemy2.activeSelf) activeCount++;

        if (counterText != null)
        {
            counterText.text = "Enemies: " + activeCount;
        }
    }

    void CheckWinCondition()
    {
        bool enemy1Defeated = enemy1 == null || !enemy1.activeSelf;
        bool enemy2Defeated = enemy2 == null || !enemy2.activeSelf;

        if (enemy1Defeated && enemy2Defeated)
        {
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