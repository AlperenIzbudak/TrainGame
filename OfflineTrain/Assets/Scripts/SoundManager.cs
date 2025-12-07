using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("Generic SFX")]
    [Tooltip("Hiç özel ses yoksa her kart atımında çalınacak default ses.")]
    public AudioClip defaultCardPlaceClip;
    
    [Tooltip("UI butonları için klik sesi.")]
    public AudioClip buttonClickClip;
    
    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("Planning SFX")]
    [Tooltip("Planning phase'de kart seçildiğinde çalınacak tek ses.")]
    public AudioClip planningCardPlaceClip;

    [Header("Play Phase Card Action SFX")]
    public AudioClip punchActionClip;
    public AudioClip fireActionClip;
    public AudioClip moveHorizontallyActionClip;
    public AudioClip moveVerticallyActionClip;
    public AudioClip collectActionClip;
    public AudioClip moveSheriffActionClip;
    public AudioClip drawAndPassActionClip;
    
    [Header("GameCard SFX")]
    [Tooltip("Her GameCard bittiğinde çalınacak tren çuf çuf sesi")]
    public AudioClip gameCardEndClip;

    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();  
        }
    }
    
    // UI butonları için – Button OnClick’ten bağlayacaksın.
    public void PlayButtonClick()
    {
        if (sfxSource == null || buttonClickClip == null) return;
        sfxSource.PlayOneShot(buttonClickClip);
    }
    
    // PLANNING PHASE: tek kart koyma sesi
    public void PlayPlanningCardPlace()
    {
        if (sfxSource == null) return;

        AudioClip clip = planningCardPlaceClip;
        if (clip == null)
            clip = defaultCardPlaceClip;   // elinde sadece 1 kart sesi varsa buraya koy

        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

// PLAY PHASE: kartın aksiyonuna göre ses
    public void PlayActionForCard(string cardKey)
    {
        if (sfxSource == null) return;

        AudioClip clip = null;

        switch (cardKey)
        {
            case "punch":            clip = punchActionClip;            break;
            case "fire":             clip = fireActionClip;             break;
            case "moveHorizontally": clip = moveHorizontallyActionClip; break;
            case "moveVertically":   clip = moveVerticallyActionClip;   break;
            case "collect":          clip = collectActionClip;          break;
            case "moveSheriff":      clip = moveSheriffActionClip;      break;
            case "drawAndPass":      clip = drawAndPassActionClip;      break;
        }

        // Hiç atanmadıysa sessiz geçebilirsin veya default kullanabilirsin
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }
    
    public void PlayGameCardEnd()
    {
        if (sfxSource != null && gameCardEndClip != null)
        {
            sfxSource.PlayOneShot(gameCardEndClip);
        }
    }

}
