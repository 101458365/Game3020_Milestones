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

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeSelf)
            {
                activeCount++;
            }
        }

        if (counterText != null)
        {
            counterText.text = "Enemies: " + activeCount;
        }
    }

    void CheckWinCondition()
    {
        bool allDefeated = true;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeSelf)
            {
                allDefeated = false;
                break;
            }
        }

        if (allDefeated)
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