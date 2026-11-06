using UnityEngine;

public class inimigoMorcego : MonoBehaviour
{
    [Header("Movimentação e Patrulha (Voo)")]
    public float velocidadePadrao = 2f;
    public float velocidadePerseguicao = 4f;
    private bool olhandoDireita = true;

    [Header("Verificação de Parede e Altura")]
    public Transform sensorParede;        
    public float distanciaRaioParede = 0.4f; 
    public Transform sensorChao;       
    public float alturaMinimaChao = 2.0f; 
    public LayerMask layerDoChao; 

    [Header("Deteção e Surpresa (Visão)")]
    public float distanciaDeteccao = 5.0f;
    public float tempoFingirIgnorar = 1.5f; 
    private float temporizadorSurpresa = 0f;
    private bool percebeuJogador = false;
    private Transform jogadorTransform;

    [Header("Ataque")]
    public float distanciaDeAtaque = 1.2f;
    public int danoDoAtaque = 10;
    private float cooldownAtaque = 0f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f; // Morcego voa, sem gravidade
        }

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
            Patrulhar();
        }
    }

    void GerenciarDeteccaoEPerseguicao(float distancia)
    {
        // Estado de surpresa (finge ignorar por 1.5s - fica parado no ar)
        if (!percebeuJogador)
        {
            rb.linearVelocity = Vector2.zero; 

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
        // 1. Se chegou perto para atacar, para e ataca
        if (distancia <= distanciaDeAtaque)
        {
            rb.linearVelocity = Vector2.zero;
            
            if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
            {
                Virar();
            }
            else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
            {
                Virar();
            }

            AtacarJogador();
            return; 
        }

        // Alinhamento horizontal com o jogador
        if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }

        // Perseguição voadora na direção do jogador
        Vector2 direcaoVoo = (jogadorTransform.position - transform.position).normalized;
        rb.linearVelocity = direcaoVoo * velocidadePerseguicao;
    }

    void Patrulhar()
    {
        bool temParedeFrente = false;
        if (sensorParede != null)
        {
            Vector2 direcaoOlhar = olhandoDireita ? Vector2.right : Vector2.left;
            temParedeFrente = Physics2D.Raycast(sensorParede.position, direcaoOlhar, distanciaRaioParede, layerDoChao);
        }

        float velocidadeVertical = Mathf.Sin(Time.time * 3f) * 1f; 

        if (sensorChao != null)
        {
            RaycastHit2D hitChao = Physics2D.Raycast(sensorChao.position, Vector2.down, alturaMinimaChao, layerDoChao);
            if (hitChao.collider != null)
            {
                // Se o chão está muito próximo, sobe para não ficar muito baixo
                velocidadeVertical = velocidadePadrao;
            }
        }

        if (temParedeFrente)
        {
            Virar();
        }

        float direcaoX = olhandoDireita ? 1f : -1f;
        rb.linearVelocity = new Vector2(direcaoX * velocidadePadrao, velocidadeVertical);
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaDeAtaque);

        if (sensorParede != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 direcao = olhandoDireita ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(sensorParede.position, sensorParede.position + (direcao * distanciaRaioParede));
        }

        if (sensorChao != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(sensorChao.position, sensorChao.position + (Vector3.down * alturaMinimaChao));
        }
    }
}
