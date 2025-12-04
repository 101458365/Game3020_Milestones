using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Rules")]
    [SerializeField] private int maxFalls = 3;
    [SerializeField] private float timeLimit = 90f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI fallsText;
    [SerializeField] private TextMeshProUGUI checkpointText;

    [Header("Scenes")]
    [SerializeField] private string loseSceneName = "LoseScene";

    [Header("Checkpoint System")]
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0, 2, -35);
    [SerializeField] private float checkpointDisplayDuration = 2f;

    private Vector3 currentCheckpoint;
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
        currentCheckpoint = defaultSpawnPosition;
        UpdateUI();

        if (checkpointText != null)
        {
            checkpointText.gameObject.SetActive(false);
        }
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

    public void SetCheckpoint(Vector3 position)
    {
        currentCheckpoint = position;
        Debug.Log($"Checkpoint saved at: {position}");

        if (checkpointText != null)
        {
            checkpointText.text = "Checkpoint Saved!";
            checkpointText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideCheckpointText));
            Invoke(nameof(HideCheckpointText), checkpointDisplayDuration);
        }
    }

    public Vector3 GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    private void HideCheckpointText()
    {
        if (checkpointText != null)
        {
            checkpointText.gameObject.SetActive(false);
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
            else
                timerText.color = Color.white;
        }

        if (fallsText != null)
        {
            fallsText.text = $"Falls: {currentFalls}/{maxFalls}";

            if (currentFalls >= maxFalls - 1)
                fallsText.color = Color.red;
            else
                fallsText.color = Color.white;
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

    public void ResetCheckpoint()
    {
        currentCheckpoint = defaultSpawnPosition;
        Debug.Log("Checkpoint reset to default spawn position");
    }
}