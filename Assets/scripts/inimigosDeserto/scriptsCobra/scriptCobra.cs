using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class scriptCobra : MonoBehaviour
{
    [Header("Orientação e Patrulha por Tempo")]
    [Tooltip("Marque se o sprite original da cobra olha para a DIREITA. Desmarque se olhar para a ESQUERDA.")]
    public bool spriteOlhaDireita = false;
    public float velocidadePatrulha = 2f;
    public float duracaoAndando = 2.0f;
    public float duracaoIdle = 1.0f;
    private bool olhandoDireita = true;
    private float contadorPatrulha = 0f;
    private bool emIdlePatrulha = false;
    private float tempoTravado = 0f;
    private float ultimaPosicaoX;
    private bool movimentoHorizontalSolicitado;
    private float direcaoMovimentoSolicitada;

    [Header("Detecção (Raycast) e Perseguição")]
    public Transform origemRaycast;
    public float distanciaDeteccao = 6.0f;
    public LayerMask layerJogador;
    public float velocidadePerseguicao = 3.5f;
    public float limiteTempoPerseguicaoTravada = 0.5f;
    private Transform jogadorTransform;
    private bool perseguindo = false;
    private float tempoBloqueioReaquisicao;

    [Header("Ataque e Hitbox")]
    public float distanciaDeAtaque = 1.3f;
    [Tooltip("Folga entre os colliders para considerar que a cobra encostou no jogador e pode atacar.")]
    public float margemContatoAtaque = 0.05f;
    public int danoDoAtaque = 10;
    public float cooldownAtaque = 1.5f;
    public float atrasoHitboxAtaque = 0.2f;
    private float contadorCooldownAtaque = 0f;
    public Vector2 tamanhoHitbox = new Vector2(0.9f, 1.4f);
    public float offsetXHitbox = 0.7f;
    public float offsetYHitbox = 0.8f;
    public float duracaoHitbox = 0.2f;
    public bool mostrarHitboxVisual = true;

    private Rigidbody2D rb;
    private Collider2D colisorCobra;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private inimigoScript scriptVida;
    private bool temParametroIsWalking;
    private bool temParametroIsIdle;
    private bool temParametroAttack;
    private bool temParametroHurt;
    private bool temEstadoHurt;
    private bool temEstadoDeath;
    private bool morteEmAndamento;
    private float tempoRestanteAnimacaoHurt;
    private readonly int estadoHurtHash = Animator.StringToHash("Base Layer.hurt");
    private readonly int estadoDeathHash = Animator.StringToHash("Base Layer.death");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisorCobra = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        scriptVida = GetComponent<inimigoScript>();
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        jogadorTransform = jogador != null ? jogador.transform : null;
        ultimaPosicaoX = rb.position.x;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            temEstadoHurt = animator.HasState(0, estadoHurtHash);
            temEstadoDeath = animator.HasState(0, estadoDeathHash);
            foreach (AnimatorControllerParameter parametro in animator.parameters)
            {
                if (parametro.name == "isWalking" && parametro.type == AnimatorControllerParameterType.Bool)
                    temParametroIsWalking = true;
                else if (parametro.name == "isIdle" && parametro.type == AnimatorControllerParameterType.Bool)
                    temParametroIsIdle = true;
                else if (parametro.name == "attack" && parametro.type == AnimatorControllerParameterType.Trigger)
                    temParametroAttack = true;
                else if (parametro.name == "hurt" && parametro.type == AnimatorControllerParameterType.Trigger)
                    temParametroHurt = true;
            }

            if (!temParametroIsWalking || !temParametroIsIdle || !temParametroAttack)
            {
                Debug.LogWarning($"{name}: o Animator Controller precisa dos parâmetros bool isWalking/isIdle e trigger attack para reproduzir as animações da cobra.", this);
            }

            if (!temParametroHurt)
            {
                Debug.LogWarning($"{name}: adicione um trigger chamado hurt ao Animator Controller e conecte-o ao estado da animação hurt.", this);
            }

            if (!temEstadoHurt)
            {
                Debug.LogWarning($"{name}: o Animator Controller precisa ter um estado chamado hurt na camada Base Layer.", this);
            }
        }

        if (rb.bodyType == RigidbodyType2D.Static)
        {
            Debug.LogWarning($"{name}: o Rigidbody2D está como Static; use Dynamic ou Kinematic para a cobra se mover.", this);
        }

        if ((rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0)
        {
            Debug.LogWarning($"{name}: a posição X do Rigidbody2D está congelada; a cobra não consegue patrulhar.", this);
        }

        if (scriptVida != null && scriptVida.vidaInimigo <= 0)
        {
            Debug.LogWarning($"{name}: vidaInimigo está em zero ou menos; a lógica de movimento será interrompida.", this);
        }

        olhandoDireita = true;
        AplicarFlip();
        emIdlePatrulha = false;
        contadorPatrulha = 0f;
        AtualizarAnimacaoMovimento(true, false);
    }

    void Update()
    {
        if (tempoRestanteAnimacaoHurt > 0f)
        {
            tempoRestanteAnimacaoHurt -= Time.deltaTime;
        }

        if (scriptVida != null && scriptVida.vidaInimigo <= 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AtualizarAnimacaoMovimento(false, false);
            return;
        }

        if (contadorCooldownAtaque > 0f)
        {
            contadorCooldownAtaque -= Time.deltaTime;
        }

        if (tempoBloqueioReaquisicao > 0f)
        {
            tempoBloqueioReaquisicao -= Time.deltaTime;
        }
        else
        {
            VerificarVisaoJogador();
        }

        if (perseguindo && jogadorTransform != null)
        {
            PerseguirOuAtacar();
        }
        else
        {
            PatrulharPorTempo();
        }
    }

    public void ReproduzirAnimacaoHurt()
    {
        if (animator == null || morteEmAndamento) return;

        if (temParametroIsWalking) animator.SetBool("isWalking", false);
        if (temParametroIsIdle) animator.SetBool("isIdle", false);

        if (temEstadoHurt)
        {
            tempoRestanteAnimacaoHurt = ObterDuracaoAnimacaoHurt();
            animator.Play(estadoHurtHash, 0, 0f);
        }
        else if (temParametroHurt)
        {
            tempoRestanteAnimacaoHurt = 0.5f;
            animator.ResetTrigger("hurt");
            animator.SetTrigger("hurt");
        }
    }

    public bool IniciarAnimacaoDeath()
    {
        if (morteEmAndamento) return true;
        if (animator == null || !temEstadoDeath) return false;

        morteEmAndamento = true;
        tempoRestanteAnimacaoHurt = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.Play(estadoDeathHash, 0, 0f);
        StartCoroutine(DestruirDepoisDoClip("death"));
        return true;
    }

    IEnumerator DestruirDepoisDoClip(string nomeClip)
    {
        yield return new WaitForSeconds(ObterDuracaoClip(nomeClip));
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (scriptVida != null && scriptVida.vidaInimigo <= 0)
        {
            tempoTravado = 0f;
            ultimaPosicaoX = rb.position.x;
            return;
        }

        float deslocamentoX = Mathf.Abs(rb.position.x - ultimaPosicaoX);
        ultimaPosicaoX = rb.position.x;

        if (!movimentoHorizontalSolicitado || deslocamentoX >= 0.001f)
        {
            tempoTravado = 0f;
            return;
        }

        tempoTravado += Time.fixedDeltaTime;
        float limiteTravado = perseguindo ? limiteTempoPerseguicaoTravada : 0.35f;
        if (tempoTravado >= limiteTravado)
        {
            tempoTravado = 0f;

            if (perseguindo)
            {
                perseguindo = false;
                jogadorTransform = null;
                tempoBloqueioReaquisicao = limiteTempoPerseguicaoTravada;
                olhandoDireita = direcaoMovimentoSolicitada < 0f;
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

    void LateUpdate()
    {
        if (animator == null || morteEmAndamento || scriptVida != null && scriptVida.vidaInimigo <= 0) return;

        bool andando = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        AtualizarAnimacaoMovimento(andando, !andando);
    }

    void VerificarVisaoJogador()
    {
        Vector2 origem = origemRaycast != null ? (Vector2)origemRaycast.position : (Vector2)transform.position;
        Vector2 direcao = olhandoDireita ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(origem, direcao, distanciaDeteccao, layerJogador);
        if (hit.collider != null)
        {
            jogadorTransform = hit.collider.transform;
            perseguindo = true;
            emIdlePatrulha = false;
            return;
        }

        if (jogadorTransform != null)
        {
            Vector2 diferenca = jogadorTransform.position - transform.position;
            float alcanceVertical = distanciaDeAtaque + tamanhoHitbox.y * 0.5f;
            if (Mathf.Abs(diferenca.x) <= distanciaDeAtaque && Mathf.Abs(diferenca.y) <= alcanceVertical)
            {
                perseguindo = true;
                emIdlePatrulha = false;
                return;
            }
        }

        if (perseguindo && jogadorTransform != null)
        {
            float dist = Vector2.Distance(transform.position, jogadorTransform.position);
            if (dist > distanciaDeteccao * 1.25f)
            {
                perseguindo = false;
                jogadorTransform = null;
                contadorPatrulha = 0f;
                emIdlePatrulha = false;
            }
        }
    }

    void PatrulharPorTempo()
    {
        contadorPatrulha += Time.deltaTime;

        if (!emIdlePatrulha)
        {
            float dirX = olhandoDireita ? 1f : -1f;
            DefinirVelocidadeHorizontal(dirX * velocidadePatrulha);
            AtualizarAnimacaoMovimento(true, false);

            if (contadorPatrulha >= duracaoAndando)
            {
                contadorPatrulha = 0f;
                emIdlePatrulha = true;
                DefinirVelocidadeHorizontal(0f);
                AtualizarAnimacaoMovimento(false, true);
            }
        }
        else
        {
            DefinirVelocidadeHorizontal(0f);
            AtualizarAnimacaoMovimento(false, true);

            if (contadorPatrulha >= duracaoIdle)
            {
                contadorPatrulha = 0f;
                emIdlePatrulha = false;
                olhandoDireita = !olhandoDireita;
                AplicarFlip();

                float dirX = olhandoDireita ? 1f : -1f;
                DefinirVelocidadeHorizontal(dirX * velocidadePatrulha);
                AtualizarAnimacaoMovimento(true, false);
            }
        }
    }

    void PerseguirOuAtacar()
    {
        Vector2 diferenca = jogadorTransform.position - transform.position;
        float distanciaHorizontal = Mathf.Abs(diferenca.x);
        float alcanceVertical = distanciaDeAtaque + tamanhoHitbox.y * 0.5f;

        if (distanciaHorizontal > distanciaDeAtaque * 0.5f)
        {
            bool jogadorNaDireita = jogadorTransform.position.x >= transform.position.x;
            if (jogadorNaDireita != olhandoDireita)
            {
                olhandoDireita = jogadorNaDireita;
                AplicarFlip();
            }
        }

        bool dentroDoAlcance = EstaDentroDoAlcanceDeAtaque(diferenca, alcanceVertical);

        if (dentroDoAlcance)
        {
            DefinirVelocidadeHorizontal(0f);
            AtualizarAnimacaoMovimento(false, true);
            ExecutarAtaque();
        }
        else
        {
            float dirX = diferenca.x >= 0f ? 1f : -1f;
            DefinirVelocidadeHorizontal(dirX * velocidadePerseguicao);
            AtualizarAnimacaoMovimento(true, false);
        }
    }

    bool EstaDentroDoAlcanceDeAtaque(Vector2 diferenca, float alcanceVertical)
    {
        Collider2D colisorJogador = jogadorTransform.GetComponent<Collider2D>();
        if (colisorJogador == null)
        {
            colisorJogador = jogadorTransform.GetComponentInParent<Collider2D>();
        }

        if (colisorCobra != null && colisorJogador != null)
        {
            return colisorCobra.Distance(colisorJogador).distance <= margemContatoAtaque;
        }

        return Mathf.Abs(diferenca.x) <= distanciaDeAtaque &&
               Mathf.Abs(diferenca.y) <= alcanceVertical;
    }

    void ExecutarAtaque()
    {
        if (contadorCooldownAtaque > 0f) return;

        contadorCooldownAtaque = cooldownAtaque;

        if (animator != null && temParametroAttack)
        {
            animator.SetTrigger("attack");
        }

        StartCoroutine(CriarHitboxAposAtraso());
    }

    IEnumerator CriarHitboxAposAtraso()
    {
        if (atrasoHitboxAtaque > 0f)
        {
            yield return new WaitForSeconds(atrasoHitboxAtaque);
        }

        CriarHitboxDeAtaque();
    }

    void CriarHitboxDeAtaque()
    {
        Vector3 posicaoHitbox = ObterCentroHitbox();

        GameObject hitboxObj = new GameObject("HitboxAtaqueCobra");
        hitboxObj.transform.position = posicaoHitbox;
        hitboxObj.layer = gameObject.layer;

        Rigidbody2D hitboxRb = hitboxObj.AddComponent<Rigidbody2D>();
        hitboxRb.bodyType = RigidbodyType2D.Kinematic;
        hitboxRb.gravityScale = 0f;

        BoxCollider2D box = hitboxObj.AddComponent<BoxCollider2D>();
        box.size = tamanhoHitbox;
        box.isTrigger = true;

        if (mostrarHitboxVisual)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.transform.SetParent(hitboxObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(tamanhoHitbox.x, tamanhoHitbox.y, 1f);
            Destroy(visual.GetComponent<Collider2D>());

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = new Color(1f, 0f, 0f, 0.45f);
            }
        }

        HitboxDanoCobra danoComp = hitboxObj.AddComponent<HitboxDanoCobra>();
        danoComp.Configurar(danoDoAtaque, layerJogador, posicaoHitbox, tamanhoHitbox);

        Destroy(hitboxObj, duracaoHitbox);
    }

    void AplicarFlip()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteOlhaDireita ? !olhandoDireita : olhandoDireita;
        }
    }

    void AtualizarAnimacaoMovimento(bool andando, bool parado)
    {
        if (animator != null && !morteEmAndamento && tempoRestanteAnimacaoHurt <= 0f)
        {
            if (temParametroIsWalking) animator.SetBool("isWalking", andando);
            if (temParametroIsIdle) animator.SetBool("isIdle", parado);
        }
    }

    float ObterDuracaoAnimacaoHurt()
    {
        return ObterDuracaoClip("hurt");
    }

    float ObterDuracaoClip(string nomeClip)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip.name == nomeClip) return Mathf.Max(clip.length, 0.05f);
            }
        }

        return 0.5f;
    }

    void DefinirVelocidadeHorizontal(float velocidade)
    {
        direcaoMovimentoSolicitada = velocidade;
        rb.linearVelocity = new Vector2(velocidade, rb.linearVelocity.y);
        movimentoHorizontalSolicitado = Mathf.Abs(velocidade) > 0.01f;
    }

    Vector2 ObterCentroHitbox()
    {
        float x = transform.position.x + (olhandoDireita ? offsetXHitbox : -offsetXHitbox);
        float y = transform.position.y + offsetYHitbox;
        return new Vector2(x, y);
    }

    private void OnDrawGizmos()
    {
        Vector3 origem = origemRaycast != null ? origemRaycast.position : transform.position;
        Vector3 dir = olhandoDireita ? Vector3.right : Vector3.left;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origem, origem + dir * distanciaDeteccao);

        Gizmos.color = Color.red;
        Vector2 centro = ObterCentroHitbox();
        Gizmos.DrawWireCube(centro, new Vector3(tamanhoHitbox.x, tamanhoHitbox.y, 1f));
    }
}

public class HitboxDanoCobra : MonoBehaviour
{
    private int dano;
    private LayerMask layerAlvo;
    private bool jaAplicouDano = false;

    public void Configurar(int danoCausado, LayerMask layerJogador, Vector2 centro, Vector2 tamanho)
    {
        dano = danoCausado;
        layerAlvo = layerJogador;
        Physics2D.SyncTransforms();

        // Aplica dano imediatamente se o jogador já estiver dentro da área da hitbox ao nascer
        Collider2D[] iniciais = Physics2D.OverlapBoxAll(centro, tamanho, 0f);

        foreach (Collider2D col in iniciais)
        {
            TentarAplicarDano(col);
            if (jaAplicouDano) break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (jaAplicouDano) return;
        TentarAplicarDano(other);
    }

    private void TentarAplicarDano(Collider2D other)
    {
        if (jaAplicouDano || other == null) return;

        combate playerCombate = other.GetComponentInParent<combate>();

        if (playerCombate != null &&
            (layerAlvo.value == 0 || ((1 << playerCombate.gameObject.layer) & layerAlvo.value) != 0))
        {
            playerCombate.TomarDano(dano);
            jaAplicouDano = true;
        }
    }
}