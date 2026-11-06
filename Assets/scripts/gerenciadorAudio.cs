using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class gerenciadorAudio : MonoBehaviour
{
    [Header("Configuração de Música")]
    public AudioClip musicaFundo;
    public bool emLoop = true;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Se verdadeiro, a música continua tocando ao mudar de cena (DontDestroyOnLoad).")]
    public bool persistirEntreCenas = true;

    private static gerenciadorAudio _instancia;
    public static gerenciadorAudio instancia
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindFirstObjectByType<gerenciadorAudio>();
            }
            return _instancia;
        }
        private set { _instancia = value; }
    }

    private AudioSource audioSource;

    void Awake()
    {
        if (persistirEntreCenas)
        {
            if (_instancia == null)
            {
                _instancia = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instancia != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.loop = emLoop;

        if (musicaFundo != null)
        {
            TocarMusica(musicaFundo, emLoop);
        }
    }

    // Toca uma nova música informada (força a reprodução imediatamente)
    public void TocarMusica(AudioClip novaMusica, bool loop)
    {
        if (novaMusica == null)
        {
            Debug.LogWarning("gerenciadorAudio: Tentativa de tocar AudioClip nulo!");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.Stop();
        audioSource.clip = novaMusica;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.Play();
        Debug.Log("gerenciadorAudio: Tocando música -> " + novaMusica.name);
    }

    // Toca uma música a partir de um tempo específico (transição de fase do boss)
    public void TocarMusicaComTempo(AudioClip novaMusica, bool loop, float tempoInicio)
    {
        if (novaMusica == null)
        {
            Debug.LogWarning("gerenciadorAudio: Tentativa de tocar AudioClip nulo com tempo!");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.Stop();
        audioSource.clip = novaMusica;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.Play();
        audioSource.time = Mathf.Clamp(tempoInicio, 0f, novaMusica.length);
        Debug.Log("gerenciadorAudio: Tocando música de fase 2 no tempo -> " + tempoInicio);
    }

    // Retorna o tempo atual de reprodução da música
    public float ObterTempoAtual()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            return audioSource.time;
        }
        return 0f;
    }

    // Para a música atual
    public void PararMusica()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // Ajusta o volume em tempo de execução
    public void AlterarVolume(float novoVolume)
    {
        volume = Mathf.Clamp01(novoVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}
