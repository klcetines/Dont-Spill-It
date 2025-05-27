using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip walkClip;
    public AudioClip giverClip;
    public AudioClip drainerClip;
    public AudioClip wellClip;

    private AudioSource audioSource;

    public AudioClip[] backgroundMusicClips;
    private int currentMusicIndex = 0;

    private AudioSource musicSource;

    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.05f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // <-- Esto mantiene el AudioManager entre escenas

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Solo inicia la música si no está sonando
        if (!musicSource.isPlaying)
            PlayBackgroundMusic();
    }

    
    void Update()
    {
        // Actualiza el volumen de la música en tiempo real
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusicClips != null && backgroundMusicClips.Length > 0)
        {
            musicSource.clip = backgroundMusicClips[currentMusicIndex];
            musicSource.Play();
            Invoke(nameof(PlayNextBackgroundMusic), musicSource.clip.length);
        }
    }

    private void PlayNextBackgroundMusic()
    {
        currentMusicIndex = (currentMusicIndex + 1) % backgroundMusicClips.Length;
        PlayBackgroundMusic();
    }

    public void PlayWalkLoop()
    {
        if (walkClip != null)
        {
            audioSource.clip = walkClip;
            audioSource.loop = true;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
    }

    public void StopWalkLoop()
    {
        if (audioSource.isPlaying && audioSource.clip == walkClip)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    public void PlayGiver()
    {
        if (giverClip != null)
            audioSource.PlayOneShot(giverClip);
    }

    public void PlayDrainer()
    {
        if (drainerClip != null)
            audioSource.PlayOneShot(drainerClip);
    }

    public void PlayWell()
    {
        if (wellClip != null)
            audioSource.PlayOneShot(wellClip);
    }
}
