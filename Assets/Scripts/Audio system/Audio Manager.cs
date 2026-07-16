using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundEffectSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = GetOrCreateAudioSource(musicSource, "Music Source");
        soundEffectSource = GetOrCreateAudioSource(soundEffectSource, "Sound Effect Source");

        musicSource.loop = true;
        soundEffectSource.loop = false;
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    /// <summary>
    /// Plays looping background music. The current track is not restarted by default.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool restart = false)
    {
        if (clip == null)
        {
            return;
        }

        if (!restart && musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    /// <summary>
    /// Plays a one-shot sound effect. Multiple effects can overlap.
    /// </summary>
    public void PlaySoundEffect(AudioClip clip)
    {
        if (clip != null)
        {
            soundEffectSource.PlayOneShot(clip);
        }
    }

    public void StopAllSoundEffects()
    {
        soundEffectSource.Stop();
    }

    private AudioSource GetOrCreateAudioSource(AudioSource source, string objectName)
    {
        if (source != null)
        {
            return source;
        }

        GameObject sourceObject = new GameObject(objectName);
        sourceObject.transform.SetParent(transform);
        return sourceObject.AddComponent<AudioSource>();
    }
}
