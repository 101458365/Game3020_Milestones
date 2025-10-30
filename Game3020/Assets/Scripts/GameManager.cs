using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Rules")]
    [SerializeField] private int maxFalls = 3;
    [SerializeField] private float timeLimit = 120f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI fallsText;

    [Header("Scenes")]
    [SerializeField] private string loseSceneName = "LoseScene";

    private int currentFalls = 0;
    private float timeRemaining;
    private bool gameOver = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        timeRemaining = timeLimit;
        UpdateUI();
    }

    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;
        UpdateUI();

        if (timeRemaining <= 0f)
        {
            LoseGame("Time's Up!");
        }
    }

    public void RegisterFall()
    {
        if (gameOver) return;

        currentFalls++;
        UpdateUI();

        Debug.Log($"Player fell! Falls: {currentFalls}/{maxFalls}");

        if (currentFalls >= maxFalls)
        {
            LoseGame("Too Many Falls!");
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";

            if (timeRemaining <= 10f)
                timerText.color = Color.red;
        }

        if (fallsText != null)
        {
            fallsText.text = $"Falls: {currentFalls}/{maxFalls}";

            if (currentFalls >= maxFalls - 1)
                fallsText.color = Color.red;
        }
    }

    void LoseGame(string reason)
    {
        gameOver = true;
        Debug.Log($"Game Over: {reason}");
        Invoke(nameof(LoadLoseScene), 1f);
    }

    void LoadLoseScene()
    {
        SceneManager.LoadScene(loseSceneName);
    }

    public bool IsGameOver()
    {
        return gameOver;
    }
}