using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class scriptUrubu : MonoBehaviour
{
    private enum EstadoVoo
    {
        Patrulha,
        Perseguindo,
        Rasante,
        Recuperacao,
        Morto
    }

    [Header("Voo e Patrulha")]
    [Tooltip("Marque se o sprite original olha para a direita. O spritesheet do urubu começa olhando para a esquerda.")]
    public bool spriteOlhaDireita = false;
    public float velocidadePatrulha = 2f;
    public float distanciaPatrulha = 4f;
    public float amplitudeOscilacao = 0.35f;
    public float frequenciaOscilacao = 2f;
    public LayerMask layerDoChao;

    [Header("Perseguicao")]
    public float distanciaDeteccao = 7f;
    public float alturaAcimaJogador = 2f;
    public float velocidadePerseguicao = 3.5f;
    public float distanciaAlinhamentoRasante = 0.5f;

    [Header("Rasante")]
    public float intervaloRasante = 2.5f;
    public float velocidadeRasante = 7f;
    public float distanciaAcerto = 0.65f;
    public float tempoMaximoRasante = 1.5f;
    public float tempoRecuperacao = 0.6f;
    public int danoDoAtaque = 10;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private inimigoScript scriptVida;
    private Transform jogadorTransform;
    private EstadoVoo estado = EstadoVoo.Patrulha;
    private Vector2 pontoInicial;
    private bool olhandoDireita = true;
    private bool jogadorAtingidoNoRasante;
    private float temporizadorRasante;
    private float temporizadorRasanteAtual;
    private float temporizadorRecuperacao;
    private string animacaoAtual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        scriptVida = GetComponent<inimigoScript>();
        pontoInicial = rb.position;
        temporizadorRasante = intervaloRasante;

        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        olhandoDireita = spriteOlhaDireita;
        AplicarOrientacao();
        ProcurarJogador();
        TocarAnimacao("walk");

        if (animator != null)
        {
            AvisarSeEstadoAusente("idle");
            AvisarSeEstadoAusente("walk");
            AvisarSeEstadoAusente("attack");
            AvisarSeEstadoAusente("death");
        }
    }

    void Update()
    {
        if (estado == EstadoVoo.Morto || (scriptVida != null && scriptVida.vidaInimigo <= 0))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (jogadorTransform == null) ProcurarJogador();

        switch (estado)
        {
            case EstadoVoo.Patrulha:
                VerificarDeteccaoJogador();
                break;
            case EstadoVoo.Perseguindo:
                AtualizarPerseguicao();
                break;
            case EstadoVoo.Rasante:
                AtualizarRasante();
                break;
            case EstadoVoo.Recuperacao:
                AtualizarRecuperacao();
                break;
        }
    }

    void FixedUpdate()
    {
        if (estado == EstadoVoo.Morto) return;

        switch (estado)
        {
            case EstadoVoo.Patrulha:
                MoverNaPatrulha();
                break;
            case EstadoVoo.Perseguindo:
                MoverAteJogadorPorCima();
                break;
            case EstadoVoo.Rasante:
                MoverNoRasante();
                break;
            case EstadoVoo.Recuperacao:
                MoverParaRecuperacao();
                break;
        }
    }

    void ProcurarJogador()
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        jogadorTransform = jogador != null ? jogador.transform : null;
    }

    void VerificarDeteccaoJogador()
    {
        if (jogadorTransform == null) return;

        if (Vector2.Distance(rb.position, jogadorTransform.position) <= distanciaDeteccao)
        {
            estado = EstadoVoo.Perseguindo;
            temporizadorRasante = intervaloRasante;
        }
    }

    void AtualizarPerseguicao()
    {
        if (jogadorTransform == null ||
            Vector2.Distance(rb.position, jogadorTransform.position) > distanciaDeteccao * 1.5f)
        {
            VoltarParaPatrulha();
            return;
        }

        temporizadorRasante -= Time.deltaTime;
        Vector2 pontoAcima = ObterPontoAcimaJogador();
        bool alinhado = Mathf.Abs(rb.position.x - pontoAcima.x) <= distanciaAlinhamentoRasante &&
                        Mathf.Abs(rb.position.y - pontoAcima.y) <= distanciaAlinhamentoRasante;

        if (temporizadorRasante <= 0f && alinhado)
        {
            estado = EstadoVoo.Rasante;
            temporizadorRasanteAtual = 0f;
            jogadorAtingidoNoRasante = false;
            TocarAnimacao("attack", true);
        }
    }

    void AtualizarRasante()
    {
        temporizadorRasanteAtual += Time.deltaTime;
        if (jogadorTransform == null || temporizadorRasanteAtual >= tempoMaximoRasante)
        {
            IniciarRecuperacao();
            return;
        }

        if (!jogadorAtingidoNoRasante &&
            Vector2.Distance(rb.position, jogadorTransform.position) <= distanciaAcerto)
        {
            AplicarDanoAoJogador();
            jogadorAtingidoNoRasante = true;
            IniciarRecuperacao();
            return;
        }

        if (rb.position.y <= jogadorTransform.position.y - distanciaAcerto)
        {
            IniciarRecuperacao();
        }
    }

    void AtualizarRecuperacao()
    {
        temporizadorRecuperacao -= Time.deltaTime;
        if (jogadorTransform == null)
        {
            VoltarParaPatrulha();
            return;
        }

        if (temporizadorRecuperacao <= 0f &&
            Vector2.Distance(rb.position, ObterPontoAcimaJogador()) <= distanciaAlinhamentoRasante)
        {
            estado = EstadoVoo.Perseguindo;
            temporizadorRasante = intervaloRasante;
        }
    }

    void MoverNaPatrulha()
    {
        float limiteDireito = pontoInicial.x + Mathf.Max(0f, distanciaPatrulha);
        float limiteEsquerdo = pontoInicial.x - Mathf.Max(0f, distanciaPatrulha);

        if (olhandoDireita && rb.position.x >= limiteDireito)
        {
            olhandoDireita = false;
            AplicarOrientacao();
        }
        else if (!olhandoDireita && rb.position.x <= limiteEsquerdo)
        {
            olhandoDireita = true;
            AplicarOrientacao();
        }

        float alvoX = olhandoDireita ? limiteDireito : limiteEsquerdo;
        float alvoY = pontoInicial.y + Mathf.Sin(Time.time * frequenciaOscilacao) * amplitudeOscilacao;
        VoarAte(new Vector2(alvoX, alvoY), velocidadePatrulha);
        AtualizarAnimacaoMovimento();
    }

    void MoverAteJogadorPorCima()
    {
        if (jogadorTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        VoarAte(ObterPontoAcimaJogador(), velocidadePerseguicao);
        AtualizarAnimacaoMovimento();
    }

    void MoverNoRasante()
    {
        if (jogadorTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        VoarAte(jogadorTransform.position, velocidadeRasante);
        AtualizarOrientacao(jogadorTransform.position.x - rb.position.x);
    }

    void MoverParaRecuperacao()
    {
        if (jogadorTransform == null)
        {
            VoarAte(pontoInicial + Vector2.up * alturaAcimaJogador, velocidadePerseguicao);
            return;
        }

        VoarAte(ObterPontoAcimaJogador(), velocidadePerseguicao);
        AtualizarAnimacaoMovimento();
    }

    void VoarAte(Vector2 destino, float velocidade)
    {
        Vector2 diferenca = destino - rb.position;
        if (diferenca.sqrMagnitude <= 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = diferenca.normalized * velocidade;
        AtualizarOrientacao(diferenca.x);
    }

    Vector2 ObterPontoAcimaJogador()
    {
        return (Vector2)jogadorTransform.position + Vector2.up * alturaAcimaJogador;
    }

    void IniciarRecuperacao()
    {
        estado = EstadoVoo.Recuperacao;
        temporizadorRecuperacao = tempoRecuperacao;
        TocarAnimacao("walk");
    }

    void AplicarDanoAoJogador()
    {
        combate combateJogador = jogadorTransform.GetComponentInParent<combate>();
        if (combateJogador != null) combateJogador.TomarDano(danoDoAtaque);
    }

    void AtualizarAnimacaoMovimento()
    {
        bool paradoEmPatrulhaNoChao = estado == EstadoVoo.Patrulha &&
                                      rb.linearVelocity.sqrMagnitude <= 0.01f &&
                                      layerDoChao.value != 0 &&
                                      rb.IsTouchingLayers(layerDoChao);

        TocarAnimacao(paradoEmPatrulhaNoChao ? "idle" : "walk");
    }

    void AtualizarOrientacao(float direcaoX)
    {
        if (Mathf.Abs(direcaoX) < 0.01f) return;
        olhandoDireita = direcaoX > 0f;
        AplicarOrientacao();
    }

    void AplicarOrientacao()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteOlhaDireita ? !olhandoDireita : olhandoDireita;
        }
    }

    void TocarAnimacao(string nomeEstado, bool reiniciar = false)
    {
        if (animator == null || (!reiniciar && animacaoAtual == nomeEstado)) return;

        int hashEstado = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hashEstado)) return;

        animator.Play(hashEstado, 0, 0f);
        animacaoAtual = nomeEstado;
    }

    void AvisarSeEstadoAusente(string nomeEstado)
    {
        int hashEstado = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hashEstado))
        {
            Debug.LogWarning($"{name}: o Animator Controller não tem o estado 'Base Layer.{nomeEstado}'.", this);
        }
    }

    public bool IniciarAnimacaoMorte()
    {
        int hashMorte = Animator.StringToHash("Base Layer.death");
        if (animator == null || !animator.HasState(0, hashMorte)) return false;

        estado = EstadoVoo.Morto;
        rb.linearVelocity = Vector2.zero;
        TocarAnimacao("death", true);
        StartCoroutine(DestruirDepoisDaAnimacao("death"));
        return true;
    }

    IEnumerator DestruirDepoisDaAnimacao(string nomeClip)
    {
        float duracao = 0.5f;
        if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == nomeClip)
                {
                    duracao = Mathf.Max(clip.length, 0.05f);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(duracao);
        Destroy(gameObject);
    }

    void VoltarParaPatrulha()
    {
        estado = EstadoVoo.Patrulha;
        jogadorTransform = null;
        pontoInicial = rb.position;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAcerto);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * alturaAcimaJogador);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pontoInicial + Vector2.left * distanciaPatrulha,
                            pontoInicial + Vector2.right * distanciaPatrulha);
        }
    }
}
