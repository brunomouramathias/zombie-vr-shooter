using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource stingSource;

    public AudioClip backgroundMusic;
    public AudioClip gameOverSting;
    public float musicVolume = 0.3f;
    public bool IsMusicActive => (musicSource != null && (musicSource.isPlaying || isMusicPlaying));
    private bool isMusicPlaying;

    void Awake()
    {
        Instance = this;
        SetupSources();
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        PlayMusic();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
        if (Instance == this) Instance = null;
    }

    private void SetupSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.spatialBlend = 0.0f; // 2D para musica ambiente
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;

        if (stingSource == null)
        {
            stingSource = gameObject.AddComponent<AudioSource>();
        }
        stingSource.spatialBlend = 0.0f; // 2D para sting de fim de jogo
        stingSource.loop = false;
        stingSource.volume = 0.7f;
        stingSource.playOnAwake = false;

        if (backgroundMusic == null)
        {
            backgroundMusic = Resources.Load<AudioClip>("Sons/background_music");
            if (backgroundMusic == null)
            {
                // Carrega diretamente de Assets/Sons se em editor/runtime
            }
        }
        if (gameOverSting == null)
        {
            gameOverSting = Resources.Load<AudioClip>("Sons/game_over_sting");
        }
    }

    public void PlayMusic()
    {
        if (musicSource != null)
        {
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>("Sons/background_music");
            }
            if (backgroundMusic != null)
            {
                backgroundMusic.LoadAudioData();
                musicSource.clip = backgroundMusic;
                musicSource.Play();
                isMusicPlaying = true;
            }
        }
    }

    public void StopMusic()
    {
        isMusicPlaying = false;
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    private void HandleGameOver()
    {
        StopMusic();

        if (stingSource != null && gameOverSting != null)
        {
            stingSource.PlayOneShot(gameOverSting);
        }
    }

    public static void ConfigureAudioSource3D(AudioSource source, float minDistance = 2.0f, float maxDistance = 30.0f)
    {
        if (source == null) return;
        source.spatialBlend = 1.0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0.0f;
    }
}

