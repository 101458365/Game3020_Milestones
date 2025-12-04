using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoafer : MonoBehaviour
{
    [SerializeField] int sceneIndex;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    public void LoadGameScene(int index)
    {
        sceneIndex = index;
        SceneManager.LoadScene(index);
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
