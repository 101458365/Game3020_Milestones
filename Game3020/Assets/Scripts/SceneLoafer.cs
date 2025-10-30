using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoafer : MonoBehaviour
{
    [SerializeField] int sceneIndex;
    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
