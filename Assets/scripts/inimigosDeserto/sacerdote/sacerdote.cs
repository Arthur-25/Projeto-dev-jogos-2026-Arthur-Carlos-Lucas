using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class sacerdote : MonoBehaviour
{
    [Header("Orientacao e Patrulha")]
    [Tooltip("Marque se o sprite original olha para a direita.")]
    public bool spriteOlhaDireita = true;
    public float velocidadePatrulha = 2f;
    public float duracaoAndando = 2f;
    public float duracaoIdle = 1f;

    [Header("Deteccao e Perseguicao")]
    public Transform origemRaycast;
    public float distanciaDeteccao = 7f;
    public LayerMask layerJogador = 1 << 7;
    public LayerMask layerObstaculos = 1 << 3;
    public float velocidadePerseguicao = 2.5f;
    public float distanciaMinimaJogador = 3f;
    public float distanciaMaximaJogador = 6f;
    public float limiteTempoTravado = 0.5f;

    [Header("Ataque a Distancia")]
    public GameObject prefabProjetil;
    public GameObject prefabMumia;
    public Transform origemProjetil;
    public float intervaloAtaque = 1.5f;
    public float atrasoDisparo = 0.2f;
    public int danoProjetil = 4;
    public float velocidadeProjetil = 8f;

    private Rigidbody2D rb;
    private Collider2D colisorSacerdote;
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
    private float contadorAtaque;
    private float tempoTravado;
    private float ultimaPosicaoX;
    private float velocidadeHorizontalSolicitada;
    private float bloqueioReaquisicao;
    private string animacaoAtual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisorSacerdote = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        scriptVida = GetComponent<inimigoScript>();
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
            DefinirVelocidadeHorizontal(0f);
            return;
        }

        if (contadorAtaque > 0f) contadorAtaque -= Time.deltaTime;
        if (bloqueioReaquisicao > 0f) bloqueioReaquisicao -= Time.deltaTime;

        if (ataqueEmAndamento)
        {
            DefinirVelocidadeHorizontal(0f);
            return;
        }

        if (jogadorTransform == null) EncontrarJogador();
        if (bloqueioReaquisicao <= 0f) VerificarVisaoJogador();

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
        if (morteEmAndamento || scriptVida != null && scriptVida.vidaInimigo <= 0)
        {
            tempoTravado = 0f;
            ultimaPosicaoX = rb.position.x;
            return;
        }

        float deslocamentoX = Mathf.Abs(rb.position.x - ultimaPosicaoX);
        ultimaPosicaoX = rb.position.x;

        if (Mathf.Abs(velocidadeHorizontalSolicitada) <= 0.1f || deslocamentoX >= 0.001f)
        {
            tempoTravado = 0f;
            return;
        }

        tempoTravado += Time.fixedDeltaTime;
        if (tempoTravado >= limiteTempoTravado)
        {
            tempoTravado = 0f;
            if (perseguindo)
            {
                perseguindo = false;
                jogadorTransform = null;
                bloqueioReaquisicao = limiteTempoTravado;
                olhandoDireita = velocidadeHorizontalSolicitada < 0f;
            }
            else
            {
                olhandoDireita = !olhandoDireita;
            }

            contadorPatrulha = 0f;
            emIdlePatrulha = false;
            AplicarFlip();
            DefinirVelocidadeHorizontal(0f);
        }
    }

    void EncontrarJogador()
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        jogadorTransform = jogador != null ? jogador.transform : null;
    }

    void VerificarVisaoJogador()
    {
        if (jogadorTransform == null) return;

        Vector2 origem = origemRaycast != null ? (Vector2)origemRaycast.position : rb.position;
        Vector2 direcao = olhandoDireita ? Vector2.right : Vector2.left;
        RaycastHit2D hitJogador = Physics2D.Raycast(origem, direcao, distanciaDeteccao, layerJogador);

        if (hitJogador.collider != null)
        {
            RaycastHit2D obstaculo = Physics2D.Raycast(origem, direcao, hitJogador.distance, layerObstaculos);
            if (obstaculo.collider == null)
            {
                combate combateJogador = hitJogador.collider.GetComponentInParent<combate>();
                jogadorTransform = combateJogador != null ? combateJogador.transform : hitJogador.collider.transform;
                perseguindo = true;
                emIdlePatrulha = false;
                return;
            }
        }

        if (perseguindo && jogadorTransform != null &&
            Vector2.Distance(rb.position, jogadorTransform.position) > distanciaDeteccao * 1.25f)
        {
            AbandonarPerseguicao(false);
        }
    }

    void Patrulhar()
    {
        contadorPatrulha += Time.deltaTime;

        if (TemObstaculoAFrente())
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
        float distancia = Mathf.Abs(diferencaX);
        if (distancia > 0.05f)
        {
            olhandoDireita = diferencaX > 0f;
            AplicarFlip();
        }

        float distanciaMinima = Mathf.Max(0f, distanciaMinimaJogador);
        float distanciaMaxima = Mathf.Max(distanciaMinima, distanciaMaximaJogador);

        if (TemObstaculoEntre(origemRaycast != null ? (Vector2)origemRaycast.position : rb.position,
                              jogadorTransform.position))
        {
            AbandonarPerseguicao(true);
            return;
        }

        if (distancia < distanciaMinima)
        {
            DefinirVelocidadeHorizontal(0f);
            if (contadorAtaque <= 0f)
            {
                StartCoroutine(RotinaAtaque(true));
            }
            else
            {
                TocarAnimacao("idle");
            }
        }
        else if (distancia > distanciaMaxima)
        {
            DefinirVelocidadeHorizontal((diferencaX >= 0f ? 1f : -1f) * velocidadePerseguicao);
            TocarAnimacao("walk");
        }
        else
        {
            DefinirVelocidadeHorizontal(0f);
            if (contadorAtaque <= 0f)
            {
                StartCoroutine(RotinaAtaque(false));
            }
            else
            {
                TocarAnimacao("idle");
            }
        }
    }

    IEnumerator RotinaAtaque(bool invocarMumia)
    {
        ataqueEmAndamento = true;
        contadorAtaque = intervaloAtaque;
        DefinirVelocidadeHorizontal(0f);
        TocarAnimacao("ataque", true);

        if (atrasoDisparo > 0f) yield return new WaitForSeconds(atrasoDisparo);
        if (invocarMumia)
        {
            InvocarMumia();
        }
        else
        {
            DispararProjetil();
        }

        float restante = Mathf.Max(0f, ObterDuracaoClip("ataque") - atrasoDisparo);
        if (restante > 0f) yield return new WaitForSeconds(restante);

        ataqueEmAndamento = false;
        if (!morteEmAndamento) TocarAnimacao(perseguindo ? "idle" : "walk");
    }

    void InvocarMumia()
    {
        if (prefabMumia == null)
        {
            Debug.LogWarning($"{name}: atribua o prefab da mumia para usar o ataque de invocacao.", this);
            return;
        }

        float ladoOpostoAoJogador = jogadorTransform != null && jogadorTransform.position.x >= transform.position.x
            ? -1f
            : 1f;
        float meiaLarguraSacerdote = colisorSacerdote != null ? colisorSacerdote.bounds.extents.x : 0.5f;
        float meiaLarguraMumia = 0.4f;
        BoxCollider2D colisorMumiaPrefab = prefabMumia.GetComponent<BoxCollider2D>();
        if (colisorMumiaPrefab != null)
        {
            meiaLarguraMumia = colisorMumiaPrefab.size.x * Mathf.Abs(prefabMumia.transform.localScale.x) * 0.5f;
        }

        Vector3 posicaoInvocacao = transform.position + Vector3.right *
            (ladoOpostoAoJogador * (meiaLarguraSacerdote + meiaLarguraMumia + 0.15f));
        Instantiate(prefabMumia, posicaoInvocacao, Quaternion.identity);
    }

    void DispararProjetil()
    {
        if (prefabProjetil == null || jogadorTransform == null)
        {
            if (prefabProjetil == null) Debug.LogWarning($"{name}: atribua o prefab de projétil do sacerdote no Inspector.", this);
            return;
        }

        Vector2 origemCandidata = origemProjetil != null
            ? (Vector2)origemProjetil.position
            : rb.position;
        Vector2 direcao = ((Vector2)jogadorTransform.position - origemCandidata).normalized;
        if (direcao.sqrMagnitude < 0.001f) direcao = olhandoDireita ? Vector2.right : Vector2.left;

        float meiaLarguraSacerdote = 0f;
        if (colisorSacerdote != null)
        {
            Bounds boundsSacerdote = colisorSacerdote.bounds;
            meiaLarguraSacerdote = Mathf.Abs(direcao.x) * boundsSacerdote.extents.x +
                                    Mathf.Abs(direcao.y) * boundsSacerdote.extents.y;
            float deslocamentoAtual = Vector2.Dot(origemCandidata - (Vector2)boundsSacerdote.center, direcao);
            if (deslocamentoAtual < meiaLarguraSacerdote + 0.05f)
            {
                origemCandidata = (Vector2)boundsSacerdote.center + direcao * (meiaLarguraSacerdote + 0.05f);
            }
        }

        GameObject projetil = Instantiate(prefabProjetil, origemCandidata, Quaternion.identity);
        Collider2D[] colisoresProjetil = projetil.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D colisorProjetil in colisoresProjetil)
        {
            if (colisorProjetil != null) colisorProjetil.isTrigger = true;
        }

        Collider2D[] colisoresSacerdote = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D colisorProjetil in colisoresProjetil)
        {
            foreach (Collider2D colisorProprio in colisoresSacerdote)
            {
                if (colisorProjetil != null && colisorProprio != null)
                {
                    Physics2D.IgnoreCollision(colisorProjetil, colisorProprio);
                }
            }
        }

        Rigidbody2D rbProjetil = projetil.GetComponent<Rigidbody2D>();
        if (rbProjetil != null)
        {
            rbProjetil.bodyType = RigidbodyType2D.Kinematic;
            rbProjetil.gravityScale = 0f;
        }

        projetilGenerico componenteProjetil = projetil.GetComponent<projetilGenerico>();
        if (componenteProjetil != null)
        {
            componenteProjetil.dano = danoProjetil;
            componenteProjetil.velocidade = velocidadeProjetil;
            componenteProjetil.rebativel = false;
            componenteProjetil.DefinirDirecao(direcao);
        }
    }

    bool TemObstaculoAFrente()
    {
        Vector2 origem = origemRaycast != null ? (Vector2)origemRaycast.position : rb.position;
        Vector2 direcao = olhandoDireita ? Vector2.right : Vector2.left;
        return layerObstaculos.value != 0 &&
               Physics2D.Raycast(origem, direcao, 0.4f, layerObstaculos).collider != null;
    }

    bool TemObstaculoEntre(Vector2 origem, Vector2 destino)
    {
        if (layerObstaculos.value == 0) return false;
        Vector2 delta = destino - origem;
        RaycastHit2D hit = Physics2D.Raycast(origem, delta.normalized, delta.magnitude, layerObstaculos);
        return hit.collider != null;
    }

    void DefinirVelocidadeHorizontal(float velocidade)
    {
        velocidadeHorizontalSolicitada = velocidade;
        rb.linearVelocity = new Vector2(velocidade, rb.linearVelocity.y);
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
        bool temDeath = animator.HasState(0, Animator.StringToHash("Base Layer.death"));
        bool temDie = animator.HasState(0, Animator.StringToHash("Base Layer.die"));
        if (!temDeath && !temDie)
        {
            Debug.LogWarning($"{name}: o Animator precisa ter o estado 'death' ou 'die'.", this);
        }
    }

    void AvisarSeEstadoAusente(string nomeEstado)
    {
        int hash = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"{name}: o Animator precisa ter o estado 'Base Layer.{nomeEstado}'.", this);
        }
    }

    public bool IniciarAnimacaoMorte()
    {
        if (morteEmAndamento) return true;
        string estadoMorte = animator != null && animator.HasState(0, Animator.StringToHash("Base Layer.death"))
            ? "death"
            : "die";
        int hash = Animator.StringToHash("Base Layer." + estadoMorte);
        if (animator == null || !animator.HasState(0, hash)) return false;

        morteEmAndamento = true;
        ataqueEmAndamento = false;
        rb.linearVelocity = Vector2.zero;
        TocarAnimacao(estadoMorte, true);
        StartCoroutine(DestruirDepoisDaMorte(estadoMorte));
        return true;
    }

    IEnumerator DestruirDepoisDaMorte(string estadoMorte)
    {
        string nomeClip = estadoMorte == "die" ? "die" : "death";
        yield return new WaitForSeconds(ObterDuracaoClip(nomeClip));
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

    void AbandonarPerseguicao(bool inverterDirecao)
    {
        perseguindo = false;
        jogadorTransform = null;
        bloqueioReaquisicao = limiteTempoTravado;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origem = origemRaycast != null ? origemRaycast.position : transform.position;
        Gizmos.DrawLine(origem, origem + (spriteOlhaDireita ? Vector3.right : Vector3.left) * distanciaDeteccao);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaMinimaJogador);
        Gizmos.DrawWireSphere(transform.position, distanciaMaximaJogador);
    }
}
