using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class teleportePonto : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private Transform pontoDestino;
    [SerializeField] private Vector3 offsetDestino = Vector3.zero;

    [Header("Identificacao do Jogador")]
    [SerializeField] private string tagDoJogador = "Player";
    [SerializeField] private bool aceitarQualquerObjetoSeSemTag = false;

    [Header("Opcoes")]
    [SerializeField] private bool zerarVelocidadeRigidbody2D = true;
    [SerializeField] private bool exigirEnterPressionadoNesteFrame = true;
    [SerializeField] private float cooldownTeleporte = 0.2f;

    private Transform jogadorAtual;
    private float proximoTeleporteDisponivel;

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
        jogadorAtual = null;
        proximoTeleporteDisponivel = 0f;
    }

    void Update()
    {
        if (jogadorAtual == null) return;
        if (Time.unscaledTime < proximoTeleporteDisponivel) return;

        if (ApertouEnter())
        {
            TeleportarJogador();
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

    private void RegistrarEntrada(GameObject outro)
    {
        if (EhJogador(outro))
        {
            jogadorAtual = outro.transform;
        }
    }

    private void RegistrarSaida(GameObject outro)
    {
        if (jogadorAtual != null && outro.transform == jogadorAtual)
        {
            jogadorAtual = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegistrarEntrada(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RegistrarSaida(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RegistrarEntrada(collision.gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        RegistrarSaida(collision.gameObject);
    }

    public void TeleportarJogador()
    {
        if (jogadorAtual == null) return;

        if (pontoDestino == null)
        {
            Debug.LogWarning($"{name}: atribua um GameObject/Transform em 'Ponto Destino' no Inspector.", this);
            return;
        }

        Vector3 destinoFinal = pontoDestino.position + offsetDestino;
        destinoFinal.z = jogadorAtual.position.z;

        Rigidbody2D rb2d = jogadorAtual.GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = jogadorAtual.GetComponentInParent<Rigidbody2D>();
        }

        if (rb2d != null)
        {
            if (zerarVelocidadeRigidbody2D)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
            rb2d.position = destinoFinal;
        }

        jogadorAtual.position = destinoFinal;
        proximoTeleporteDisponivel = Time.unscaledTime + cooldownTeleporte;
    }

    void OnDrawGizmos()
    {
        if (pontoDestino == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pontoDestino.position + offsetDestino, 0.25f);
        Gizmos.DrawLine(transform.position, pontoDestino.position + offsetDestino);
    }
}