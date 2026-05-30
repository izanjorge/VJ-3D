using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente entre escenas que gestiona toda la música y los SFX.
/// Arrastra el GameObject "AudioManager" a la primera escena cargada en Build Settings.
/// Asigna los clips desde el Inspector; después llama AudioManager.Instancia.PlaySFX(clip)
/// desde cualquier script.
///
/// Mapeo automático de música por índice de escena (Build Settings):
///   0         → musicaMenu
///   1         → musicaCreditos
///   2 – 7     → musicaNiveles
///   8 – 10    → musicaPeligro
///   11        → musicaBoss
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instancia { get; private set; }

    // ── Música ────────────────────────────────────────────────────────────────
    [Header("Música")]
    public AudioClip musicaMenu;
    public AudioClip musicaNiveles;
    public AudioClip musicaPeligro;
    public AudioClip musicaBoss;
    public AudioClip musicaCreditos;

    // ── Jugador ───────────────────────────────────────────────────────────────
    [Header("Jugador")]
    public AudioClip sfxJugadorPaso;
    public AudioClip sfxSalto;
    public AudioClip sfxJugadorAtaque;
    public AudioClip sfxJugadorImpacto;
    public AudioClip sfxJugadorDanio;
    public AudioClip sfxJugadorMuerte;
    public AudioClip sfxJugadorRalentizado;
    public AudioClip sfxMoneda;

    // ── Slime ─────────────────────────────────────────────────────────────────
    [Header("Slime")]
    public AudioClip sfxSlimePaso;
    public AudioClip sfxMuerteGenerica;       // sfx_muerte.wav → muerte del Slime

    // ── Esqueleto ─────────────────────────────────────────────────────────────
    [Header("Esqueleto")]
    public AudioClip sfxEsqueletoPaso;
    public AudioClip sfxEsqueletoAtaque;
    public AudioClip sfxEsqueletoMuerte;

    // ── Panda ─────────────────────────────────────────────────────────────────
    [Header("Panda")]
    public AudioClip sfxCuchillo;             // sfx_panda_cuchillo.wav — compartido con Metagross
    public AudioClip sfxPandaMuerte;

    // ── Metagross ─────────────────────────────────────────────────────────────
    [Header("Metagross")]
    public AudioClip sfxMetaEntrada;          // sfx_meta_entrada.wav        — caída desde el cielo
    public AudioClip sfxMetaAterrizaje;       // sfx_meta_aterrizaje.wav     — impacto al aterrizar
    public AudioClip sfxMetaCargando;         // sfx_meta_cargando.wav       — fase de preparación
    public AudioClip sfxMetaDanio;            // sfx_meta_danio.wav          — recibe un golpe
    public AudioClip sfxMetaMuerteExplosion;  // sfx_meta_muerte_explosion.wav — muerte épica

    // ── Trampas y Mundo ───────────────────────────────────────────────────────
    [Header("Trampas y Mundo")]
    public AudioClip sfxFlechaDisparo;
    public AudioClip sfxPinchos;
    public AudioClip sfxJarronRomper;
    public AudioClip sfxCajaEmpujar;
    public AudioClip sfxCajaGolpe;
    public AudioClip sfxPuerta;
    public AudioClip sfxSueloAdvertencia;
    public AudioClip sfxSueloCaida;

    // ── Volúmenes ─────────────────────────────────────────────────────────────
    [Header("Volúmenes — Música")]
    [Range(0f, 1f)] public float volumenMusicaMenu     = 0.45f;
    [Range(0f, 1f)] public float volumenMusicaNiveles  = 0.2f;
    [Range(0f, 1f)] public float volumenMusicaPeligro  = 0.2f;
    [Range(0f, 1f)] public float volumenMusicaBoss     = 0.2f;
    [Range(0f, 1f)] public float volumenMusicaCreditos = 0.45f;

    [Header("Volúmenes — SFX")]
    [Range(0f, 1f)] public float volumenSFX    = 0.7f;   // volumen base de todos los SFX
    [Range(0f, 1f)] public float volumenMoneda = 1f;     // la moneda suena más alta que el resto

    private AudioSource musicaSource;
    private AudioSource sfxSource;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        // Un AudioSource para música en loop y otro para SFX superpuestos
        musicaSource      = gameObject.AddComponent<AudioSource>();
        musicaSource.loop = true;

        sfxSource        = gameObject.AddComponent<AudioSource>();
        sfxSource.loop   = false;
        sfxSource.volume = volumenSFX;
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnEscenaCargada; }
    void OnDisable() { SceneManager.sceneLoaded -= OnEscenaCargada; }

    void Start()
    {
        // Reproduce la música correspondiente a la escena inicial
        ActualizarMusica(SceneManager.GetActiveScene().buildIndex);
    }

    void OnEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        ActualizarMusica(escena.buildIndex);
    }

    // ── Lógica de música por escena ───────────────────────────────────────────
    void ActualizarMusica(int buildIndex)
    {
        AudioClip clip   = null;
        float     volumen = 0.45f;

        if      (buildIndex == 0)                    { clip = musicaMenu;     volumen = volumenMusicaMenu; }
        else if (buildIndex == 1)                    { clip = musicaCreditos; volumen = volumenMusicaCreditos; }
        else if (buildIndex >= 2 && buildIndex <= 7) { clip = musicaNiveles;  volumen = volumenMusicaNiveles; }
        else if (buildIndex >= 8 && buildIndex <= 10){ clip = musicaPeligro;  volumen = volumenMusicaPeligro; }
        else if (buildIndex == 11)                   { clip = musicaBoss;     volumen = volumenMusicaBoss; }

        if (clip == null) return;                    // Sin música asignada: silencio
        if (musicaSource.clip == clip) return;       // Ya está sonando: no interrumpir

        musicaSource.clip   = clip;
        musicaSource.volume = volumen;
        musicaSource.Play();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Reproduce un SFX al volumen base configurado.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumenSFX);
    }

    /// <summary>Reproduce un SFX con escala de volumen personalizada (útil para la moneda, muertes, etc.).</summary>
    public void PlaySFX(AudioClip clip, float escala)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, escala);
    }

    /// <summary>Cambia la música manualmente con volumen personalizado.</summary>
    public void PlayMusica(AudioClip clip, float volumen = 0.45f)
    {
        if (clip == null || musicaSource.clip == clip) return;
        musicaSource.clip   = clip;
        musicaSource.volume = volumen;
        musicaSource.Play();
    }

    /// <summary>Para la música actual.</summary>
    public void StopMusica() => musicaSource.Stop();
}
