using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class kingOfDesert : MonoBehaviour
{
    [System.Serializable]
    public struct ConfigHitboxCombo
    {
        public string rotulo;
        [Tooltip("Tempo de espera (startup) antes desta hitbox aparecer e aplicar dano.")]
        public float delayAparicao;
        [Tooltip("Tempo que a hitbox visual e o intervalo pos-golpe permanecem ativos.")]
        public float duracao;
        [Tooltip("Offset local em relacao ao boss (X positivo = frente, X negativo = tras).")]
        public Vector2 offset;
        public Vector2 tamanho;
        public int dano;
    }

    private enum EstadoBoss
    {
        Inativo,
        Perseguindo,
        ExecutandoAcao,
        Morto
    }

    [Header("UI da Vida do Boss")]
    [SerializeField] private Image barraPreenchimentoBoss;
    [SerializeField] private GameObject barraDeVidaCanvasObj;
    [SerializeField] private float duracaoFadeBarraVida = 1f;

    [Header("Musicas do Boss")]
    [SerializeField] private AudioClip musicaBossFase1;
    [SerializeField] private AudioClip musicaBossFase2;

    [Header("Segunda Fase Dinamica")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float porcentagemSegundaFase = 0.5f;
    [SerializeField] private float multiplicadorVelocidadeFase2 = 1.3f;
    [SerializeField] private float multiplicadorCooldownFase2 = 0.7f;
    [SerializeField] private bool teleporteAoAtivarSegundaFase = false;

    [Header("Ativacao por GameObject")]
    [Tooltip("GameObject (com Collider2D Trigger ou normal) que ao ser tocado pelo jogador acorda o King of Sands.")]
    [SerializeField] private GameObject gatilhoAcordar;
    [SerializeField] private string tagJogador = "Player";

    [Header("Orientacao e Movimento")]
    [Tooltip("O sprite original do King of Sands olha para a direita.")]
    [SerializeField] private bool spriteOlhaDireita = true;
    [SerializeField] private float velocidadeMovimento = 3.5f;
    [SerializeField] private float distanciaAlcanceCombo = 2.4f;
    [SerializeField] private float distanciaMinimaLonge = 6.5f;
    [SerializeField] private float cooldownEntreAcoes = 1.2f;

    [Header("Combo (2 Hitboxes: Tras e Frente)")]
    [SerializeField] private bool mostrarHitbox = true;
    [SerializeField] private ConfigHitboxCombo hitbox1Tras = new ConfigHitboxCombo
    {
        rotulo = "Golpe 1 (Tras)",
        delayAparicao = 0.25f,
        duracao = 0.15f,
        offset = new Vector2(-1.2f, 0f),
        tamanho = new Vector2(1.8f, 1.4f),
        dano = 15
    };
    [SerializeField] private ConfigHitboxCombo hitbox2Frente = new ConfigHitboxCombo
    {
        rotulo = "Golpe 2 (Frente)",
        delayAparicao = 0.35f,
        duracao = 0.15f,
        offset = new Vector2(1.4f, 0f),
        tamanho = new Vector2(2.2f, 1.6f),
        dano = 25
    };

    [Header("Ground Attack (Projetil)")]
    [SerializeField] private GameObject prefabProjetil;
    [SerializeField] private Transform pontoSpawnProjetil;
    [SerializeField] private float delayCriarProjetil = 0.4f;
    [SerializeField] private float posDelayGroundAttack = 0.4f;
    [SerializeField] private int projeteisAntesDoTeleporte = 3;

    [Header("Teleporte")]
    [Tooltip("Tempo de startup da animacao de teleporte antes de mover o boss para perto do jogador.")]
    [SerializeField] private float startupTeleporte = 0.5f;
    [Tooltip("Tempo apos teleportar antes de retomar a perseguicao/ataque.")]
    [SerializeField] private float posDelayTeleporte = 0.25f;
    [Tooltip("Distancia horizontal ao lado do jogador onde o boss reaparece.")]
    [SerializeField] private float distanciaAposTeleporte = 2.0f;

    [Header("Morte")]
    [SerializeField] private float tempoFallbackMorte = 2.0f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private inimigoScript scriptVida;
    private Transform jogadorTransform;

    private EstadoBoss estado = EstadoBoss.Inativo;
    private bool acordou;
    private bool segundaFase;
    private bool olhandoDireita = true;
    private int vidaMaximaReferencia;
    private int projeteisDisparadosLonge;
    private float timerCooldownAcao;
    private string animacaoAtual;
    private Coroutine rotinaAtual;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        scriptVida = GetComponentInChildren<inimigoScript>();

        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        if (scriptVida != null)
        {
            vidaMaximaReferencia = Mathf.Max(1, scriptVida.vidaInimigo);
        }

        if (barraDeVidaCanvasObj != null)
        {
            barraDeVidaCanvasObj.SetActive(false);
        }

        olhandoDireita = spriteOlhaDireita;
        AplicarOrientacao();
        ConfigurarGatilhoAcordar();
        AtualizarBarraVida();
    }

    void Start()
    {
        ProcurarJogador();
        estado = EstadoBoss.Inativo;
        TocarAnimacao("idle", true);

        if (animator != null)
        {
            AvisarSeEstadoAusente("idle");
            AvisarSeEstadoAusente("teleport");
            AvisarSeEstadoAusente("walk");
            AvisarSeEstadoAusente("death");
            AvisarSeEstadoAusente("combo");
            AvisarSeEstadoAusente("groundAttack");
        }
    }

    void Update()
    {
        if (estado == EstadoBoss.Morto) return;

        int vidaAtual = ObterVidaAtual();
        AtualizarBarraVida(vidaAtual);

        if (scriptVida != null && vidaAtual <= 0)
        {
            IniciarAnimacaoMorte();
            return;
        }

        if (jogadorTransform == null)
        {
            ProcurarJogador();
        }

        if (!acordou || estado == EstadoBoss.Inativo)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            TocarAnimacao("idle");
            return;
        }

        if (!segundaFase && vidaMaximaReferencia > 0 && vidaAtual <= vidaMaximaReferencia * porcentagemSegundaFase)
        {
            AtivarSegundaFase();
        }

        if (estado == EstadoBoss.ExecutandoAcao) return;

        if (timerCooldownAcao > 0f)
        {
            timerCooldownAcao -= Time.deltaTime;
        }

        if (jogadorTransform == null)
        {
            PararMovimentoHorizontal();
            TocarAnimacao("idle");
            return;
        }

        float distancia = Vector2.Distance(transform.position, jogadorTransform.position);

        if (distancia <= distanciaAlcanceCombo)
        {
            PararMovimentoHorizontal();
            VirarParaJogador();

            if (timerCooldownAcao <= 0f)
            {
                IniciarRotina(RotinaCombo());
            }
            else
            {
                TocarAnimacao("idle");
            }
            return;
        }

        if (distancia >= distanciaMinimaLonge)
        {
            PararMovimentoHorizontal();
            VirarParaJogador();

            if (timerCooldownAcao <= 0f)
            {
                if (projeteisDisparadosLonge >= projeteisAntesDoTeleporte)
                {
                    IniciarRotina(RotinaTeleporte());
                }
                else
                {
                    IniciarRotina(RotinaGroundAttack());
                }
            }
            else
            {
                TocarAnimacao("idle");
            }
            return;
        }

        MoverParaJogador();
    }

    void FixedUpdate()
    {
        if (estado != EstadoBoss.Perseguindo || jogadorTransform == null) return;

        float diffX = jogadorTransform.position.x - transform.position.x;
        float dirX = Mathf.Abs(diffX) < 0.08f ? 0f : Mathf.Sign(diffX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(dirX * velocidadeMovimento, rb.linearVelocity.y);
        }
    }

    private int ObterVidaAtual()
    {
        if (scriptVida == null) return vidaMaximaReferencia;

        if (scriptVida.vidaInimigo > vidaMaximaReferencia)
        {
            vidaMaximaReferencia = scriptVida.vidaInimigo;
        }

        return Mathf.Max(0, scriptVida.vidaInimigo);
    }

    private void AtualizarBarraVida(int? vidaOverride = null)
    {
        if (barraPreenchimentoBoss == null || vidaMaximaReferencia <= 0) return;
        int atual = vidaOverride ?? ObterVidaAtual();
        barraPreenchimentoBoss.fillAmount = Mathf.Clamp01((float)atual / vidaMaximaReferencia);
    }

    private void AtivarSegundaFase()
    {
        if (segundaFase || estado == EstadoBoss.Morto) return;
        segundaFase = true;

        velocidadeMovimento *= multiplicadorVelocidadeFase2;
        cooldownEntreAcoes = Mathf.Max(0.15f, cooldownEntreAcoes * multiplicadorCooldownFase2);

        if (gerenciadorAudio.instancia != null && musicaBossFase2 != null)
        {
            gerenciadorAudio.instancia.TocarMusica(musicaBossFase2, true);
        }

        if (teleporteAoAtivarSegundaFase && estado != EstadoBoss.ExecutandoAcao)
        {
            IniciarRotina(RotinaTeleporte());
        }
    }

    private void MoverParaJogador()
    {
        estado = EstadoBoss.Perseguindo;
        VirarParaJogador();
        TocarAnimacao("walk");
    }

    private void PararMovimentoHorizontal()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private IEnumerator RotinaCombo()
    {
        estado = EstadoBoss.ExecutandoAcao;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        VirarParaJogador();
        TocarAnimacao("combo", true);

        if (hitbox1Tras.delayAparicao > 0f)
        {
            yield return new WaitForSeconds(hitbox1Tras.delayAparicao);
        }
        if (estado == EstadoBoss.Morto) yield break;
        AplicarHitbox(hitbox1Tras);
        if (hitbox1Tras.duracao > 0f)
        {
            yield return new WaitForSeconds(hitbox1Tras.duracao);
        }

        if (estado == EstadoBoss.Morto) yield break;
        VirarParaJogador();

        if (hitbox2Frente.delayAparicao > 0f)
        {
            yield return new WaitForSeconds(hitbox2Frente.delayAparicao);
        }
        if (estado == EstadoBoss.Morto) yield break;
        AplicarHitbox(hitbox2Frente);
        if (hitbox2Frente.duracao > 0f)
        {
            yield return new WaitForSeconds(hitbox2Frente.duracao);
        }

        if (jogadorTransform != null && Vector2.Distance(transform.position, jogadorTransform.position) < distanciaMinimaLonge)
        {
            projeteisDisparadosLonge = 0;
        }

        FinalizarAcao();
    }

    private IEnumerator RotinaGroundAttack()
    {
        estado = EstadoBoss.ExecutandoAcao;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        VirarParaJogador();
        TocarAnimacao("groundAttack", true);

        if (delayCriarProjetil > 0f)
        {
            yield return new WaitForSeconds(delayCriarProjetil);
        }
        if (estado == EstadoBoss.Morto) yield break;

        CriarProjetil();
        projeteisDisparadosLonge++;

        if (posDelayGroundAttack > 0f)
        {
            yield return new WaitForSeconds(posDelayGroundAttack);
        }

        FinalizarAcao();
    }

    private IEnumerator RotinaTeleporte()
    {
        estado = EstadoBoss.ExecutandoAcao;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        TocarAnimacao("teleport", true);

        if (startupTeleporte > 0f)
        {
            yield return new WaitForSeconds(startupTeleporte);
        }
        if (estado == EstadoBoss.Morto) yield break;

        if (jogadorTransform != null)
        {
            float lado = jogadorTransform.position.x >= transform.position.x ? -1f : 1f;
            Vector3 novoDestino = new Vector3(
                jogadorTransform.position.x + lado * distanciaAposTeleporte,
                jogadorTransform.position.y,
                transform.position.z
            );

            if (rb != null)
            {
                rb.position = novoDestino;
            }
            transform.position = novoDestino;
            VirarParaJogador();
        }

        projeteisDisparadosLonge = 0;

        if (posDelayTeleporte > 0f)
        {
            yield return new WaitForSeconds(posDelayTeleporte);
        }

        FinalizarAcao();
    }

    private void CriarProjetil()
    {
        if (prefabProjetil == null)
        {
            Debug.LogWarning($"{name}: atribua um 'Prefab Projetil' no Inspector para usar no groundAttack.", this);
            return;
        }

        Vector3 spawnPos = pontoSpawnProjetil != null ? pontoSpawnProjetil.position : transform.position;
        Instantiate(prefabProjetil, spawnPos, Quaternion.identity);
    }

    private void AplicarHitbox(ConfigHitboxCombo cfg)
    {
        float direcaoX = olhandoDireita ? 1f : -1f;
        Vector2 centro = new Vector2(
            transform.position.x + cfg.offset.x * direcaoX,
            transform.position.y + cfg.offset.y
        );

        if (mostrarHitbox && cfg.duracao > 0f)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = $"Hitbox_{cfg.rotulo}";
            visual.transform.position = new Vector3(centro.x, centro.y, transform.position.z);
            visual.transform.localScale = new Vector3(
                Mathf.Max(0.05f, cfg.tamanho.x),
                Mathf.Max(0.05f, cfg.tamanho.y),
                1f
            );
            Collider2D colVisual = visual.GetComponent<Collider2D>();
            if (colVisual != null) Destroy(colVisual);
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(1f, 0.2f, 0.1f, 0.65f);
            Destroy(visual, cfg.duracao);
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, cfg.tamanho, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(tagJogador))
            {
                combate combateJogador = hit.GetComponentInParent<combate>();
                if (combateJogador != null)
                {
                    combateJogador.TomarDano(cfg.dano);
                }
            }
        }
    }

    private void FinalizarAcao()
    {
        if (estado == EstadoBoss.Morto) return;
        estado = EstadoBoss.Perseguindo;
        timerCooldownAcao = cooldownEntreAcoes;
        rotinaAtual = null;
        TocarAnimacao("idle");
    }

    private void IniciarRotina(IEnumerator novaRotina)
    {
        if (rotinaAtual != null)
        {
            StopCoroutine(rotinaAtual);
        }
        rotinaAtual = StartCoroutine(novaRotina);
    }

    public void AcordarBoss()
    {
        if (acordou || estado == EstadoBoss.Morto) return;
        acordou = true;
        estado = EstadoBoss.Perseguindo;
        projeteisDisparadosLonge = 0;
        ProcurarJogador();

        if (scriptVida != null)
        {
            vidaMaximaReferencia = Mathf.Max(1, scriptVida.vidaInimigo);
        }

        if (barraDeVidaCanvasObj != null)
        {
            barraDeVidaCanvasObj.SetActive(true);
        }
        AtualizarBarraVida();

        if (gerenciadorAudio.instancia != null && musicaBossFase1 != null)
        {
            gerenciadorAudio.instancia.TocarMusica(musicaBossFase1, true);
        }

        TocarAnimacao("walk", true);
    }

    public bool IniciarAnimacaoMorte()
    {
        if (estado == EstadoBoss.Morto) return true;
        estado = EstadoBoss.Morto;
        acordou = false;
        AtualizarBarraVida(0);

        if (rotinaAtual != null)
        {
            StopCoroutine(rotinaAtual);
            rotinaAtual = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        TocarAnimacao("death", true);
        StartCoroutine(FadeOutBarraVida());
        StartCoroutine(DestruirAposAnimacaoMorte());
        return true;
    }

    private IEnumerator FadeOutBarraVida()
    {
        if (barraDeVidaCanvasObj == null) yield break;

        CanvasGroup canvasGroup = barraDeVidaCanvasObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = barraDeVidaCanvasObj.AddComponent<CanvasGroup>();
        }

        float tempo = Mathf.Max(0.05f, duracaoFadeBarraVida);
        float contador = 0f;

        while (contador < tempo)
        {
            contador += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, contador / tempo);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        barraDeVidaCanvasObj.SetActive(false);
    }

    private IEnumerator DestruirAposAnimacaoMorte()
    {
        float duracao = tempoFallbackMorte;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "death")
                {
                    duracao = Mathf.Max(clip.length, 0.1f);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(duracao);
        Destroy(gameObject);
    }

    private void ConfigurarGatilhoAcordar()
    {
        if (gatilhoAcordar == null) return;

        GatilhoAcordarKing encaminhador = gatilhoAcordar.GetComponent<GatilhoAcordarKing>();
        if (encaminhador == null)
        {
            encaminhador = gatilhoAcordar.AddComponent<GatilhoAcordarKing>();
        }
        encaminhador.Inicializar(this, tagJogador);
    }

    internal void NotificarContatoGatilho(GameObject outro)
    {
        if (acordou || estado == EstadoBoss.Morto) return;
        if (outro != null && outro.CompareTag(tagJogador))
        {
            AcordarBoss();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gatilhoAcordar == null)
        {
            NotificarContatoGatilho(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gatilhoAcordar == null)
        {
            NotificarContatoGatilho(collision.gameObject);
        }
    }

    private void ProcurarJogador()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tagJogador);
        if (obj != null)
        {
            jogadorTransform = obj.transform;
        }
    }

    private void VirarParaJogador()
    {
        if (jogadorTransform == null) return;
        float diffX = jogadorTransform.position.x - transform.position.x;
        if (Mathf.Abs(diffX) < 0.05f) return;

        olhandoDireita = diffX > 0f;
        AplicarOrientacao();
    }

    private void AplicarOrientacao()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteOlhaDireita ? !olhandoDireita : olhandoDireita;
        }
    }

    private void TocarAnimacao(string nomeEstado, bool reiniciar = false)
    {
        if (animator == null) return;
        if (!reiniciar && animacaoAtual == nomeEstado) return;

        int hash = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hash)) return;

        animator.Play(hash, 0, 0f);
        animacaoAtual = nomeEstado;
    }

    private void AvisarSeEstadoAusente(string nomeEstado)
    {
        int hash = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"{name}: estado 'Base Layer.{nomeEstado}' nao encontrado no Animator Controller.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaAlcanceCombo);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaMinimaLonge);

        float dirX = Application.isPlaying
            ? (olhandoDireita ? 1f : -1f)
            : (spriteOlhaDireita ? 1f : -1f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Vector3 c1 = transform.position + new Vector3(hitbox1Tras.offset.x * dirX, hitbox1Tras.offset.y, 0f);
        Gizmos.DrawWireCube(c1, hitbox1Tras.tamanho);

        Gizmos.color = Color.red;
        Vector3 c2 = transform.position + new Vector3(hitbox2Frente.offset.x * dirX, hitbox2Frente.offset.y, 0f);
        Gizmos.DrawWireCube(c2, hitbox2Frente.tamanho);
    }
}

[DisallowMultipleComponent]
internal class GatilhoAcordarKing : MonoBehaviour
{
    private kingOfDesert boss;
    private string tagJogador = "Player";

    public void Inicializar(kingOfDesert alvo, string tag)
    {
        boss = alvo;
        if (!string.IsNullOrEmpty(tag))
        {
            tagJogador = tag;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (boss != null && other.CompareTag(tagJogador))
        {
            boss.NotificarContatoGatilho(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (boss != null && collision.gameObject.CompareTag(tagJogador))
        {
            boss.NotificarContatoGatilho(collision.gameObject);
        }
    }
}