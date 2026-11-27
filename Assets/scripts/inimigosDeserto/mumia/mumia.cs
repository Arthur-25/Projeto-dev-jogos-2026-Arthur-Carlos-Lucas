using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class mumia : MonoBehaviour
{
    [Header("Orientacao e Patrulha")]
    [Tooltip("Marque se o sprite original da mumia olha para a direita.")]
    public bool spriteOlhaDireita = true;
    public float velocidadePatrulha = 2f;
    public float duracaoAndando = 2f;
    public float duracaoIdle = 1f;

    [Header("Deteccao e Perseguicao")]
    public Transform origemRaycast;
    public float distanciaDeteccao = 6f;
    public LayerMask layerJogador = 1 << 7;
    public float velocidadePerseguicao = 3.5f;
    public LayerMask layerDoChao = 1 << 3;
    public float distanciaRaioParede = 0.4f;
    public float limiteTempoTravado = 0.5f;

    [Header("Ataque")]
    public float distanciaDeAtaque = 1.1f;
    public float margemContatoAtaque = 0.05f;
    public int danoDoAtaque = 10;
    public float cooldownAtaque = 1.5f;
    public float atrasoHitboxAtaque = 0.2f;
    public Vector2 tamanhoHitbox = new Vector2(1f, 0.7f);
    public float offsetXHitbox = 0.65f;
    public float offsetYHitbox = 0.05f;
    public float duracaoHitbox = 0.2f;

    private Rigidbody2D rb;
    private Collider2D colisorMumia;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private inimigoScript scriptVida;
    private Transform jogadorTransform;
    private bool olhandoDireita;
    private bool perseguindo;
    private bool emIdlePatrulha;
    private bool ataqueEmAndamento;
    private bool morteEmAndamento;
    private float contadorPatrulha;
    private float contadorCooldownAtaque;
    private float tempoTravado;
    private float ultimaPosicaoX;
    private float velocidadeHorizontalSolicitada;
    private string animacaoAtual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisorMumia = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        scriptVida = GetComponent<inimigoScript>();

        rb.gravityScale = 1f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        ultimaPosicaoX = rb.position.x;
        olhandoDireita = spriteOlhaDireita;
        AplicarFlip();
        EncontrarJogador();
        TocarAnimacao("idle");
        VerificarEstadosAnimator();
    }

    void Update()
    {
        if (morteEmAndamento) return;

        if (scriptVida != null && scriptVida.vidaInimigo <= 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (contadorCooldownAtaque > 0f)
        {
            contadorCooldownAtaque -= Time.deltaTime;
        }

        if (ataqueEmAndamento)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (jogadorTransform == null) EncontrarJogador();
        VerificarVisaoJogador();

        if (perseguindo && jogadorTransform != null)
        {
            PerseguirOuAtacar();
        }
        else
        {
            Patrulhar();
        }
    }

    void FixedUpdate()
    {
        float deslocamentoX = Mathf.Abs(rb.position.x - ultimaPosicaoX);
        ultimaPosicaoX = rb.position.x;

        bool tentandoPerseguir = perseguindo && !ataqueEmAndamento &&
                                 Mathf.Abs(velocidadeHorizontalSolicitada) > 0.1f;
        if (tentandoPerseguir && deslocamentoX < 0.001f)
        {
            tempoTravado += Time.fixedDeltaTime;
            if (tempoTravado >= limiteTempoTravado)
            {
                AbandonarPerseguicao(true);
            }
        }
        else
        {
            tempoTravado = 0f;
        }
    }

    void EncontrarJogador()
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        jogadorTransform = jogador != null ? jogador.transform : null;
    }

    void VerificarVisaoJogador()
    {
        Vector2 origem = origemRaycast != null ? (Vector2)origemRaycast.position : (Vector2)transform.position;
        Vector2 direcao = olhandoDireita ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(origem, direcao, distanciaDeteccao, layerJogador);

        if (hit.collider != null)
        {
            RaycastHit2D obstaculo = Physics2D.Raycast(origem, direcao, hit.distance, layerDoChao);
            if (obstaculo.collider != null)
            {
                if (perseguindo) AbandonarPerseguicao(true);
                return;
            }

            jogadorTransform = hit.collider.GetComponentInParent<combate>() != null
                ? hit.collider.GetComponentInParent<combate>().transform
                : hit.collider.transform;
            perseguindo = true;
            emIdlePatrulha = false;
            return;
        }

        if (perseguindo && jogadorTransform != null &&
            Vector2.Distance(transform.position, jogadorTransform.position) > distanciaDeteccao * 1.25f)
        {
            perseguindo = false;
            jogadorTransform = null;
            contadorPatrulha = 0f;
            emIdlePatrulha = false;
        }
    }

    void Patrulhar()
    {
        contadorPatrulha += Time.deltaTime;

        if (TemParedeAFrente())
        {
            olhandoDireita = !olhandoDireita;
            AplicarFlip();
            emIdlePatrulha = false;
            contadorPatrulha = 0f;
        }

        if (!emIdlePatrulha)
        {
            DefinirVelocidadeHorizontal((olhandoDireita ? 1f : -1f) * velocidadePatrulha);
            TocarAnimacao("walk");

            if (contadorPatrulha >= duracaoAndando)
            {
                contadorPatrulha = 0f;
                emIdlePatrulha = true;
                DefinirVelocidadeHorizontal(0f);
                TocarAnimacao("idle");
            }
        }
        else
        {
            DefinirVelocidadeHorizontal(0f);
            TocarAnimacao("idle");

            if (contadorPatrulha >= duracaoIdle)
            {
                contadorPatrulha = 0f;
                emIdlePatrulha = false;
                olhandoDireita = !olhandoDireita;
                AplicarFlip();
                DefinirVelocidadeHorizontal((olhandoDireita ? 1f : -1f) * velocidadePatrulha);
                TocarAnimacao("walk");
            }
        }
    }

    void PerseguirOuAtacar()
    {
        float diferencaX = jogadorTransform.position.x - transform.position.x;
        if (Mathf.Abs(diferencaX) > 0.05f)
        {
            olhandoDireita = diferencaX > 0f;
            AplicarFlip();
        }

        if (TemParedeAFrente())
        {
            AbandonarPerseguicao(true);
            return;
        }

        if (EstaDentroDoAlcance())
        {
            DefinirVelocidadeHorizontal(0f);
            TocarAnimacao("idle");
            if (contadorCooldownAtaque <= 0f)
            {
                StartCoroutine(RotinaAtaque());
            }
        }
        else
        {
            DefinirVelocidadeHorizontal((diferencaX >= 0f ? 1f : -1f) * velocidadePerseguicao);
            TocarAnimacao("walk");
        }
    }

    bool EstaDentroDoAlcance()
    {
        Collider2D colisorJogador = jogadorTransform.GetComponent<Collider2D>();
        if (colisorJogador == null) colisorJogador = jogadorTransform.GetComponentInParent<Collider2D>();

        if (colisorMumia != null && colisorJogador != null)
        {
            return colisorMumia.Distance(colisorJogador).distance <= margemContatoAtaque;
        }

        return Mathf.Abs(jogadorTransform.position.x - transform.position.x) <= distanciaDeAtaque;
    }

    IEnumerator RotinaAtaque()
    {
        ataqueEmAndamento = true;
        contadorCooldownAtaque = cooldownAtaque;
        DefinirVelocidadeHorizontal(0f);
        TocarAnimacao("ataque", true);

        if (atrasoHitboxAtaque > 0f) yield return new WaitForSeconds(atrasoHitboxAtaque);
        AplicarDanoNaHitbox();

        float duracaoAtaque = ObterDuracaoClip("ataque");
        float tempoRestante = Mathf.Max(0f, duracaoAtaque - atrasoHitboxAtaque);
        if (tempoRestante > 0f) yield return new WaitForSeconds(tempoRestante);

        ataqueEmAndamento = false;
        if (!morteEmAndamento && (scriptVida == null || scriptVida.vidaInimigo > 0))
        {
            TocarAnimacao(perseguindo ? "walk" : "idle");
        }
    }

    void AplicarDanoNaHitbox()
    {
        Vector2 centro = new Vector2(
            transform.position.x + (olhandoDireita ? offsetXHitbox : -offsetXHitbox),
            transform.position.y + offsetYHitbox);

        Collider2D[] alvos = layerJogador.value != 0
            ? Physics2D.OverlapBoxAll(centro, tamanhoHitbox, 0f, layerJogador)
            : Physics2D.OverlapBoxAll(centro, tamanhoHitbox, 0f);

        foreach (Collider2D alvo in alvos)
        {
            combate combateJogador = alvo.GetComponentInParent<combate>();
            if (combateJogador != null)
            {
                combateJogador.TomarDano(danoDoAtaque);
                break;
            }
        }
    }

    void DefinirVelocidadeHorizontal(float velocidade)
    {
        velocidadeHorizontalSolicitada = velocidade;
        rb.linearVelocity = new Vector2(velocidade, rb.linearVelocity.y);
    }

    bool TemParedeAFrente()
    {
        if (layerDoChao.value == 0) return false;

        Vector2 origem = origemRaycast != null ? (Vector2)origemRaycast.position : (Vector2)transform.position;
        Vector2 direcao = olhandoDireita ? Vector2.right : Vector2.left;
        return Physics2D.Raycast(origem, direcao, distanciaRaioParede, layerDoChao).collider != null;
    }

    void AbandonarPerseguicao(bool inverterDirecao)
    {
        perseguindo = false;
        jogadorTransform = null;
        tempoTravado = 0f;
        contadorPatrulha = 0f;
        emIdlePatrulha = false;

        if (inverterDirecao)
        {
            olhandoDireita = !olhandoDireita;
            AplicarFlip();
        }

        DefinirVelocidadeHorizontal(0f);
    }

    void AplicarFlip()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteOlhaDireita ? !olhandoDireita : olhandoDireita;
        }
    }

    void TocarAnimacao(string nomeEstado, bool reiniciar = false)
    {
        if (animator == null || (!reiniciar && animacaoAtual == nomeEstado)) return;

        int hash = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hash)) return;

        animator.Play(hash, 0, 0f);
        animacaoAtual = nomeEstado;
    }

    void VerificarEstadosAnimator()
    {
        if (animator == null) return;
        AvisarSeEstadoAusente("idle");
        AvisarSeEstadoAusente("walk");
        AvisarSeEstadoAusente("ataque");
        AvisarSeEstadoAusente("death");
    }

    void AvisarSeEstadoAusente(string nomeEstado)
    {
        int hash = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"{name}: o Animator Controller precisa ter o estado 'Base Layer.{nomeEstado}'.", this);
        }
    }

    public bool IniciarAnimacaoMorte()
    {
        int hash = Animator.StringToHash("Base Layer.death");
        if (morteEmAndamento) return true;
        if (animator == null || !animator.HasState(0, hash)) return false;

        morteEmAndamento = true;
        ataqueEmAndamento = false;
        rb.linearVelocity = Vector2.zero;
        TocarAnimacao("death", true);
        StartCoroutine(DestruirDepoisDaMorte());
        return true;
    }

    IEnumerator DestruirDepoisDaMorte()
    {
        yield return new WaitForSeconds(ObterDuracaoClip("death"));
        Destroy(gameObject);
    }

    float ObterDuracaoClip(string nomeClip)
    {
        RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip.name == nomeClip) return Mathf.Max(clip.length, 0.05f);
            }
        }

        return 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origem = origemRaycast != null ? origemRaycast.position : transform.position;
        Gizmos.DrawLine(origem, origem + (spriteOlhaDireita ? Vector3.right : Vector3.left) * distanciaDeteccao);

        Gizmos.color = Color.red;
        Vector3 centro = transform.position + new Vector3(olhandoDireita ? offsetXHitbox : -offsetXHitbox, offsetYHitbox, 0f);
        Gizmos.DrawWireCube(centro, tamanhoHitbox);
    }
}
