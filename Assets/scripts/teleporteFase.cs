using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class teleporteFase : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private string nomeDaFase = "";

    [Header("Identificacao do Jogador")]
    [SerializeField] private string tagDoJogador = "Player";
    [SerializeField] private bool aceitarQualquerObjetoSeSemTag = false;

    [Header("Opcoes")]
    [SerializeField] private bool resetarTimeScale = true;
    [SerializeField] private bool exigirEnterPressionadoNesteFrame = true;

    private bool jogadorDentro;
    private bool carregandoCena;

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnEnable()
    {
        jogadorDentro = false;
        carregandoCena = false;
    }

    void Update()
    {
        if (!jogadorDentro || carregandoCena) return;

        bool apertouEnter = ApertouEnter();
        if (apertouEnter)
        {
            CarregarFase();
        }
    }

    private bool ApertouEnter()
    {
        if (Keyboard.current == null) return false;

        if (exigirEnterPressionadoNesteFrame)
        {
            return Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        }

        return Keyboard.current.enterKey.isPressed
            || Keyboard.current.numpadEnterKey.isPressed;
    }

    private bool EhJogador(GameObject outro)
    {
        if (string.IsNullOrEmpty(tagDoJogador))
        {
            return aceitarQualquerObjetoSeSemTag;
        }

        return outro.CompareTag(tagDoJogador);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EhJogador(other.gameObject))
        {
            jogadorDentro = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (EhJogador(other.gameObject))
        {
            jogadorDentro = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (EhJogador(collision.gameObject))
        {
            jogadorDentro = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (EhJogador(collision.gameObject))
        {
            jogadorDentro = false;
        }
    }

    public void CarregarFase()
    {
        if (carregandoCena) return;

        if (string.IsNullOrWhiteSpace(nomeDaFase))
        {
            Debug.LogWarning($"{name}: informe o 'Nome Da Fase' no Inspector.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nomeDaFase))
        {
            Debug.LogError($"{name}: a cena '{nomeDaFase}' nao esta incluida ou habilitada no Build Profile.", this);
            return;
        }

        carregandoCena = true;

        if (resetarTimeScale)
        {
            Time.timeScale = 1f;
        }

        SceneManager.LoadScene(nomeDaFase);
    }
}