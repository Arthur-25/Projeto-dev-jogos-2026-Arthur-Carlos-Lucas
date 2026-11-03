using UnityEngine;

public class scriptCachorro : MonoBehaviour
{
    [Header("Movimentação e Patrulha")]
    public float velocidadePadrao = 2f;
    public float velocidadeCorrida = 4f;
    private bool olhandoDireita = true;

    [Header("Verificação de Chão e Parede")]
    public Transform sensorChao;       
    public float distanciaRaioChao = 0.5f;
    
    public Transform sensorParede;        
    public float distanciaRaioParede = 0.4f; 
    
    public LayerMask layerDoChao; 

    [Header("Deteção e Surpresa (Visão)")]
    public float distanciaDeteccao = 5.0f;
    public float tempoFingirIgnorar = 1.5f; 
    private float temporizadorSurpresa = 0f;
    private bool percebeuJogador = false;
    private Transform jogadorTransform;

    [Header("Ataque e Pulo no Bloco")]
    public float distanciaDeAtaque = 1.2f;
    public int danoDoAtaque = 10;
    private float cooldownAtaque = 0f;
    public float forçaPulo = 12f;       
    public float velocidadeNoAr = 5f; 
    public float cooldownPuloMax = 0.8f; 
    private float cooldownPuloTimer = 0f;

    private bool estaPulando = false;

    // Temporizador para detetar travamento por inatividade no chão
    private float tempoTravado = 0f;
    private const float limiteTempoTravado = 0.5f;

    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jogadorTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (jogadorTransform == null) return;

        if (cooldownAtaque > 0f)
        {
            cooldownAtaque -= Time.deltaTime;
        }

        if (cooldownPuloTimer > 0f)
        {
            cooldownPuloTimer -= Time.deltaTime;
        }

        float distanciaAteJogador = Vector2.Distance(transform.position, jogadorTransform.position);
        bool jogadorDetectado = distanciaAteJogador <= distanciaDeteccao;

        if (jogadorDetectado)
        {
            GerenciarDeteccaoEPerseguicao(distanciaAteJogador);
        }
        else
        {
            percebeuJogador = false;
            temporizadorSurpresa = 0f;
            estaPulando = false;
            tempoTravado = 0f; 
            Patrulhar();
        }
    }

    void GerenciarDeteccaoEPerseguicao(float distancia)
    {
        // Estado de surpresa (finge ignorar por 1.5s - fica parado no Idle)
        if (!percebeuJogador)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AtivarAnimator(false, false, false, true);

            temporizadorSurpresa += Time.deltaTime;
            if (temporizadorSurpresa >= tempoFingirIgnorar)
            {
                percebeuJogador = true; 
            }
            return;
        }

        PerseguirEAtacar(distancia);
    }

    void PerseguirEAtacar(float distancia)
    {
        // 1. Se chegou perto para atacar, trava o movimento, olha para o jogador e ataca (evita o flick)
        if (distancia <= distanciaDeAtaque)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            
            if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
            {
                Virar();
            }
            else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
            {
                Virar();
            }

            AtivarAnimator(false, false, false, true); 
            AtacarJogador();
            return; 
        }

        // 2. Se estiver no ar, apenas controla a física
        if (estaPulando)
        {
            float direcaoXNoAr = olhandoDireita ? 1f : -1f;
            rb.linearVelocity = new Vector2(direcaoXNoAr * velocidadeNoAr, rb.linearVelocity.y);

            bool chaoAbaixo = sensorChao != null && Physics2D.Raycast(sensorChao.position, Vector2.down, distanciaRaioChao, layerDoChao);
            
            if ((rb.linearVelocity.y <= 0.1f && chaoAbaixo) || Mathf.Abs(rb.linearVelocity.y) < 0.05f)
            {
                estaPulando = false;
                tempoTravado = 0f; 
                AtivarAnimator(true, true, false, false); 
            }
            return;
        }

        // Verifica se há chão logo abaixo neste exato momento
        bool chaoAtual = sensorChao != null && Physics2D.Raycast(sensorChao.position, Vector2.down, distanciaRaioChao, layerDoChao);

        // Lógica de Timeout
        if (distancia > distanciaDeAtaque && Mathf.Abs(rb.linearVelocity.x) < 0.1f && chaoAtual && !estaPulando)
        {
            tempoTravado += Time.deltaTime;
            if (tempoTravado >= limiteTempoTravado)
            {
                percebeuJogador = false;
                temporizadorSurpresa = 0f;
                tempoTravado = 0f;
                Patrulhar();
                return;
            }
        }
        else
        {
            tempoTravado = 0f; 
        }

        bool temChaoFrente = true;
        if (sensorChao != null)
        {
            temChaoFrente = Physics2D.Raycast(sensorChao.position, Vector2.down, distanciaRaioChao, layerDoChao);
        }

        bool temParedeFrente = false;
        if (sensorParede != null)
        {
            Vector2 direcaoOlhar = olhandoDireita ? Vector2.right : Vector2.left;
            temParedeFrente = Physics2D.Raycast(sensorParede.position, direcaoOlhar, distanciaRaioParede, layerDoChao);
        }

        // Se encontrar buraco/borda, salta
        if (!temChaoFrente && cooldownPuloTimer <= 0f && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            AtivarAnimator(false, false, true, false); 
            estaPulando = true;
            cooldownPuloTimer = cooldownPuloMax;
            
            float direcaoPulo = olhandoDireita ? 1f : -1f;
            rb.linearVelocity = new Vector2(direcaoPulo * velocidadeNoAr, forçaPulo);
            return;
        }

        // Se encontrar parede à frente
        if (temParedeFrente)
        {
            if (jogadorTransform.position.y > transform.position.y + 0.5f && cooldownPuloTimer <= 0f && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            {
                AtivarAnimator(false, false, true, false); 
                estaPulando = true; 
                cooldownPuloTimer = cooldownPuloMax; 
                
                float direcaoPuloAlto = olhandoDireita ? 1f : -1f;
                rb.linearVelocity = new Vector2(direcaoPuloAlto * velocidadeNoAr, forçaPulo);
                return;
            }
            else
            {
                // Efeito Pong Instantâneo
                Virar();
                AtivarAnimator(true, true, false, false); 
                
                float direcaoReverso = olhandoDireita ? 1f : -1f;
                rb.linearVelocity = new Vector2(direcaoReverso * velocidadeCorrida, rb.linearVelocity.y);
                return;
            }
        }

        // Alinhamento horizontal com o jogador (durante a perseguição)
        if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }

        // PERSEGUIÇÃO A CORRER
        AtivarAnimator(true, true, false, false); 

        float direcaoX = olhandoDireita ? 1f : -1f;
        rb.linearVelocity = new Vector2(direcaoX * velocidadeCorrida, rb.linearVelocity.y);
    }

    void Patrulhar()
    {
        if (estaPulando) return;

        bool temChaoFrente = true;
        if (sensorChao != null)
        {
            temChaoFrente = Physics2D.Raycast(sensorChao.position, Vector2.down, distanciaRaioChao, layerDoChao);
        }

        bool temParedeFrente = false;
        if (sensorParede != null)
        {
            Vector2 direcaoOlhar = olhandoDireita ? Vector2.right : Vector2.left;
            temParedeFrente = Physics2D.Raycast(sensorParede.position, direcaoOlhar, distanciaRaioParede, layerDoChao);
        }

        if (!temChaoFrente || temParedeFrente)
        {
            Virar(); 
        }

        float direcaoX = olhandoDireita ? 1f : -1f;
        rb.linearVelocity = new Vector2(direcaoX * velocidadePadrao, rb.linearVelocity.y);

        bool aAndar = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        AtivarAnimator(aAndar, false, false, !aAndar); 
    }

    void AtivarAnimator(bool running, bool runningFast, bool jumping, bool idle)
    {
        if (animator != null)
        {
            animator.SetBool("isRunning", running);
            animator.SetBool("isRunningFast", runningFast);
            animator.SetBool("isJumping", jumping);
            animator.SetBool("isIdle", idle);
        }
    }

    void AtacarJogador()
    {
        if (cooldownAtaque <= 0f)
        {
            combate playerCombate = jogadorTransform.GetComponent<combate>();
            if (playerCombate != null)
            {
                playerCombate.TomarDano(danoDoAtaque);
            }
            cooldownAtaque = 1.5f;
        }
    }

    void Virar()
    {
        olhandoDireita = !olhandoDireita;
        Vector3 escala = transform.localScale;
        escala.x *= -1f;
        transform.localScale = escala;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);

        if (sensorChao != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(sensorChao.position, sensorChao.position + (Vector3.down * distanciaRaioChao));
        }

        if (sensorParede != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 direcao = olhandoDireita ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(sensorParede.position, sensorParede.position + (direcao * distanciaRaioParede));
        }
    }
}