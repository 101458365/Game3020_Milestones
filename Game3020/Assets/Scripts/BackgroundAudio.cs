using UnityEngine;

public class BackgroundAudio : MonoBehaviour
{
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float volume = 0.05f;

    private AudioSource audioSource;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.volume = volume;
        audioSource.loop = true;
    }

    void Start()
    {
        if (backgroundMusic != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }

}
