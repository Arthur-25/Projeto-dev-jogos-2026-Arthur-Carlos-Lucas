using UnityEngine;

public class scriptInimigoTutorial : MonoBehaviour
{
    [Header("Movimentação e Patrulha")]
    public float velocidadePadrao = 2f;
    public float velocidadeCorrida = 4f;
    private bool olhandoDireita = true;

    [Header("Verificação de Chão e Parede")]
    public Transform sensorChao;       
    public float distanciaRaioChao = 0.5f;
    
    public Transform sensorParede;        
    public float distanciaRaioParede = 0.3f; 
    
    public LayerMask layerDoChao; 

    [Header("Deteção de Raio Visual")]
    public float distanciaDeteccao = 5.0f;
    private Transform jogadorTransform;

    [Header("Ataque")]
    public float distanciaDeAtaque = 1.2f;
    public int danoDoAtaque = 10;
    private float cooldownAtaque = 0f;

    // Controlo do bloqueio por parede/bloco ao ver o jogador
    private bool bloqueadoPorBloco = false;
    private float temporizadorBloqueioBloco = 0f;
    private const float tempoMaximoBloqueio = 06f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

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
        bool jogadorDetectado = VerificarDeteccaoRaio(distanciaAteJogador);

        // Se estiver no estado de espera após esbarrar no bloco vendo o jogador
        if (bloqueadoPorBloco)
        {
            ControlarBloqueioBloco(jogadorDetectado);
            return;
        }

        if (jogadorDetectado)
        {
            PerseguirEAtacar(distanciaAteJogador);
        }
        else
        {
            Patrulhar();
        }
    }

    bool VerificarDeteccaoRaio(float distancia)
    {
        return distancia <= distanciaDeteccao;
    }

    void PerseguirEAtacar(float distancia)
    {
        // 1. Verifica se há buraco na frente
        bool temChaoFrente = true;
        if (sensorChao != null)
        {
            temChaoFrente = Physics2D.Raycast(sensorChao.position, Vector2.down, distanciaRaioChao, layerDoChao);
        }

        // 2. Verifica se bateu de cara numa parede/bloco alto
        bool temParedeFrente = false;
        if (sensorParede != null)
        {
            Vector2 direcaoOlhar = olhandoDireita ? Vector2.right : Vector2.left;
            temParedeFrente = Physics2D.Raycast(sensorParede.position, direcaoOlhar, distanciaRaioParede, layerDoChao);
        }

        // Se encontrar um buraco, dá meia-volta e patrulha
        if (!temChaoFrente)
        {
            Virar();
            Patrulhar();
            return;
        }

        // CONDIÇÃO EXATA: Vê o jogador E deteta bloco na frente com o sensor de parede
        if (temParedeFrente)
        {
            bloqueadoPorBloco = true;
            temporizadorBloqueioBloco = tempoMaximoBloqueio;
            return;
        }

        // Olha na direção correta do jogador
        if (jogadorTransform.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (jogadorTransform.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }

        // Se estiver dentro da distância de ataque, para e ataca
        if (distancia <= distanciaDeAtaque)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AtacarJogador();
        }
        else
        {
            float direcaoX = olhandoDireita ? 1f : -1f;
            rb.linearVelocity = new Vector2(direcaoX * velocidadeCorrida, rb.linearVelocity.y);
        }
    }

    void ControlarBloqueioBloco(bool jogadorDetectado)
    {
        // Fica parado na mesma posição
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Se o jogador sair de cima do bloco ou mudar de posição, permitindo a perseguição novamente
        bool temParedeFrente = false;
        if (sensorParede != null)
        {
            Vector2 direcaoOlhar = olhandoDireita ? Vector2.right : Vector2.left;
            temParedeFrente = Physics2D.Raycast(sensorParede.position, direcaoOlhar, distanciaRaioParede, layerDoChao);
        }

        // Se a parede sumiu (jogador saiu de cima ou desceu), cancela o bloqueio e volta a perseguir
        if (!temParedeFrente && jogadorDetectado)
        {
            bloqueadoPorBloco = false;
            return;
        }

        // Conta os 10 segundos parado
        temporizadorBloqueioBloco -= Time.deltaTime;
        if (temporizadorBloqueioBloco <= 0f)
        {
            // Passou o tempo: desiste, desativa o bloqueio e volta à movimentação padrão (patrulha/dar meia-volta)
            bloqueadoPorBloco = false;
            Virar();
        }
    }

    void Patrulhar()
    {
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
    }

    void AtacarJogador()
    {
        if (cooldownAtaque <= 0f)
        {
            combate playerCombate = jogadorTransform.GetComponent<combate>();
            if (playerCombate != null)
            {
                playerCombate.TomarDano(danoDoAtaque);
                Debug.Log("Inimigo desferiu ataque de " + danoDoAtaque + " de dano!");
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