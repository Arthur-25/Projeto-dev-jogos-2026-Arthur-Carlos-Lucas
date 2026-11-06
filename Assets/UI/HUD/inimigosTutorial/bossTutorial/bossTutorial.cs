using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class bossTutorial : MonoBehaviour
{
    [Header("UI & Vida")]
    public Image barraPreenchimentoBoss;
    public GameObject barraDeVidaCanvasObj;

    private inimigoScript scriptInimigo;
    private int vidaMaxima;
    private int vidaAtual;
    private int vidaInimigoAnterior;
    private bool estaMorto = false;

    [Header("Ativação (Estilo FromSoftware)")]
    public Transform pontoEntradaArena;
    public float distanciaInteracaoEntrada = 3f;
    public float tempoEsperaEntrada = 5f;
    private float contadorEspera = 0f;
    private bool esperandoAtivacao = false;
    private bool bossAtivo = false;

    [Header("Segunda Fase")]
    [Range(0.1f, 0.9f)] public float porcentagemSegundaFase = 0.5f;
    private bool segundaFase = false;
    public float multiplicadorVelocidadeFase2 = 1.3f;

    [Header("Movimentação & Alvo")]
    public float velocidadeMovimento = 3f;
    private Transform jogadorTransform;
    private Rigidbody2D rb;
    private Animator animator;
    private bool olhandoDireita = true;

    [Header("Combate & Golpes")]
    public float alcanceAtaqueCorpo = 1.5f;
    public int danoSideSwing = 15;
    public int danoFwdSwing = 20;
    public int danoDwnSwing = 25;
    public float cooldownAtaque = 2f;
    private float timerCooldownAtaque = 0f;

    [Header("Projétil (Fwd_Swing)")]
    public GameObject projetilPrefab;
    public Transform pontoDisparo;

    [Header("Invocações (Minions)")]
    public GameObject morcegoPrefab;
    public GameObject cachorroPrefab;
    public Transform pontoInvocacao;
    public float cooldownInvocacao = 5f;
    private float timerInvocacao = 0f;
    public float distanciaMinimaAtaqueCorpo = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // Procura o inimigoScript no próprio objeto ou em filhos
        scriptInimigo = GetComponentInChildren<inimigoScript>();

        if (scriptInimigo != null)
        {
            vidaMaxima = scriptInimigo.vidaInimigo;
            vidaAtual = vidaMaxima;
            vidaInimigoAnterior = vidaMaxima;
        }
        else
        {
            vidaMaxima = 200;
            vidaAtual = vidaMaxima;
            vidaInimigoAnterior = vidaMaxima;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jogadorTransform = playerObj.transform;
        }

        if (barraDeVidaCanvasObj != null)
        {
            barraDeVidaCanvasObj.SetActive(false);
        }
        AtualizarUI();
    }

    public void TomarDano(int dano)
    {
        if (scriptInimigo != null)
        {
            scriptInimigo.TomarDano(dano);
        }
        else
        {
            vidaAtual -= dano;
            AtualizarUI();
            if (animator != null) animator.SetTrigger("Hurt");
            if (vidaAtual <= 0) Morrer();
        }
    }

    void Update()
    {
        if (estaMorto || jogadorTransform == null) return;

        // Sincroniza a vida diretamente com o inimigoScript a cada frame
        if (scriptInimigo != null && bossAtivo)
        {
            vidaAtual = scriptInimigo.vidaInimigo;
            AtualizarUI();

            if (vidaAtual < vidaInimigoAnterior)
            {
                if (animator != null) animator.SetTrigger("Hurt");
                vidaInimigoAnterior = vidaAtual;
            }

            if (vidaAtual <= 0)
            {
                Morrer();
                return;
            }
        }
        else if (scriptInimigo == null && bossAtivo && !estaMorto)
        {
            // Se o objeto filho com o inimigoScript foi destruído no golpe final
            vidaAtual = 0;
            AtualizarUI();
            Morrer();
            return;
        }

        // Ativação da arena estilo FromSoftware
        if (!bossAtivo)
        {
            GerenciarAtivacaoArena();
            return;
        }

        if (timerCooldownAtaque > 0f) timerCooldownAtaque -= Time.deltaTime;
        if (timerInvocacao > 0f) timerInvocacao -= Time.deltaTime;

        if (!segundaFase && vidaAtual <= vidaMaxima * porcentagemSegundaFase)
        {
            AtivarSegundaFase();
        }

        ComportamentoBoss();
    }

    void GerenciarAtivacaoArena()
    {
        if (jogadorTransform == null) return;

        float distanciaEntrada = pontoEntradaArena != null ? Vector2.Distance(jogadorTransform.position, pontoEntradaArena.position) : Vector2.Distance(jogadorTransform.position, transform.position);

        if (distanciaEntrada <= distanciaInteracaoEntrada)
        {
            bool apertouEnter = (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.Return);

            if (!esperandoAtivacao && apertouEnter)
            {
                esperandoAtivacao = true;
                contadorEspera = 0f;
                Debug.Log("Entrada da arena acionada pelo jogador! Aguardando " + tempoEsperaEntrada + " segundos...");
            }
        }

        if (esperandoAtivacao)
        {
            contadorEspera += Time.deltaTime;
            if (contadorEspera >= tempoEsperaEntrada)
            {
                bossAtivo = true;
                esperandoAtivacao = false;
                if (barraDeVidaCanvasObj != null)
                {
                    barraDeVidaCanvasObj.SetActive(true);
                }
                Debug.Log("Boss Ativado após o tempo de espera!");
            }
        }
    }

    void AtivarSegundaFase()
    {
        segundaFase = true;
        velocidadeMovimento *= multiplicadorVelocidadeFase2;
        cooldownAtaque *= 0.7f;
        Debug.Log("SEGUNDA FASE DO BOSS ATIVADA! O Rei Esqueleto enfurece-se e agora invoca Cães do Inferno!");
        if (animator != null) animator.SetTrigger("Hurt");
    }

    void ComportamentoBoss()
    {
        float distanciaJogador = Vector2.Distance(transform.position, jogadorTransform.position);

        FlipParaJogador();

        if (distanciaJogador > distanciaMinimaAtaqueCorpo)
        {
            Vector2 direcao = (jogadorTransform.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direcao.x * velocidadeMovimento, rb.linearVelocity.y);

            if (animator != null) animator.SetBool("isWalking", true);

            if (timerInvocacao <= 0f)
            {
                InvocarMinions();
                timerInvocacao = cooldownInvocacao;
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null) animator.SetBool("isWalking", false);

            if (timerCooldownAtaque <= 0f)
            {
                ExecutarAtaqueAleatorio();
                timerCooldownAtaque = cooldownAtaque;
            }
        }
    }

    void ExecutarAtaqueAleatorio()
    {
        int escolhaAtaque = Random.Range(0, 3);
        if (animator == null) return;

        switch (escolhaAtaque)
        {
            case 0:
                animator.SetTrigger("SideSwing");
                AplicarDanoCorpoAPorco(danoSideSwing, 2f);
                break;
            case 1:
                animator.SetTrigger("FwdSwing");
                DispararProjetil();
                break;
            case 2:
                animator.SetTrigger("DwnSwing");
                AplicarDanoArea360(danoDwnSwing, 3.5f);
                break;
        }
    }

    void AplicarDanoCorpoAPorco(int dano, float alcance)
    {
        if (jogadorTransform == null) return;
        float dist = Vector2.Distance(transform.position, jogadorTransform.position);
        if (dist <= alcance)
        {
            combate playerCombate = jogadorTransform.GetComponent<combate>();
            if (playerCombate != null)
            {
                playerCombate.TomarDano(dano);
            }
        }
    }

    void DispararProjetil()
    {
        if (projetilPrefab != null && pontoDisparo != null)
        {
            GameObject proj = Instantiate(projetilPrefab, pontoDisparo.position, Quaternion.identity);
            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null)
            {
                float direcaoX = olhandoDireita ? 1f : -1f;
                rbProj.linearVelocity = new Vector2(direcaoX * 8f, 0f);
            }
        }
    }

    void AplicarDanoArea360(int dano, float raio)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, raio);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                combate playerCombate = col.GetComponent<combate>();
                if (playerCombate != null)
                {
                    playerCombate.TomarDano(dano);
                }
            }
        }
    }

    void InvocarMinions()
    {
        Transform localSpawn = pontoInvocacao != null ? pontoInvocacao : transform;

        if (!segundaFase)
        {
            if (morcegoPrefab != null)
            {
                Instantiate(morcegoPrefab, localSpawn.position + new Vector3(Random.Range(-1f, 1f), 1f, 0f), Quaternion.identity);
            }
        }
        else
        {
            if (cachorroPrefab != null)
            {
                Instantiate(cachorroPrefab, localSpawn.position + new Vector3(Random.Range(-1f, 1f), 0f, 0f), Quaternion.identity);
            }
        }
    }

    void FlipParaJogador()
    {
        if (jogadorTransform == null) return;

        if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }
    }

    void Virar()
    {
        olhandoDireita = !olhandoDireita;
        Vector3 escala = transform.localScale;
        escala.x *= -1f;
        transform.localScale = escala;
    }

    void AtualizarUI()
    {
        if (barraPreenchimentoBoss != null && vidaMaxima > 0)
        {
            barraPreenchimentoBoss.fillAmount = Mathf.Clamp01((float)vidaAtual / vidaMaxima);
        }
    }

    void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;

        if (animator != null) animator.SetTrigger("Die");
        bossAtivo = false;

        // Congela o Rigidbody como Kinematic para ele parar no ar/chão e não cair pelo cenário
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Mantém o colisor ativo para que ele permaneça apoiado no chão

        if (scriptInimigo != null) scriptInimigo.enabled = false;

        StartCoroutine(FadeOutBarraVida());
        Destroy(gameObject, 2.5f);
    }

    IEnumerator FadeOutBarraVida()
    {
        if (barraDeVidaCanvasObj == null) yield break;

        CanvasGroup canvasGroup = barraDeVidaCanvasObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = barraDeVidaCanvasObj.AddComponent<CanvasGroup>();
        }

        float tempoFade = 1f;
        float contador = 0f;

        while (contador < tempoFade)
        {
            contador += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, contador / tempoFade);
            yield return null;
        }

        barraDeVidaCanvasObj.SetActive(false);
    }
}
