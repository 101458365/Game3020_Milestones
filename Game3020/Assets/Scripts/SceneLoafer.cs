using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoafer : MonoBehaviour
{
    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void LoadGameScene()
    {
        SceneManager.LoadScene("SampleScene");
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
