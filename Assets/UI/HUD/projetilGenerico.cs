using UnityEngine;
using UnityEngine.InputSystem;

public class projetilGenerico : MonoBehaviour
{
    [Header("Atributos Básicos do Projétil")]
    public float velocidade = 8f;
    public int dano = 15;
    public float tempoParaDesaparecer = 4f;
    public bool desapareceAoColidir = true;

    [Header("Comportamento Teleguiado (Homing)")]
    public bool teleguiado = false;
    public float velocidadeRotacao = 200f;
    private Transform alvoTransform;

    [Header("Mecânica de Rebater (Parry)")]
    public bool rebativel = true;
    private bool rebatidoPeloJogador = false;

    [Header("Identificação")]
    public bool pertenceAoJogador = false; // Se falso, causa dano no jogador; se verdadeiro, causa dano nos inimigos

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento = Vector2.right;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        // Destrói após o tempo limite
        Destroy(gameObject, tempoParaDesaparecer);

        // Se for teleguiado, busca o alvo
        if (teleguiado)
        {
            if (pertenceAoJogador)
            {
                GameObject inimigoObj = GameObject.FindGameObjectWithTag("Inimigo");
                if (inimigoObj != null) alvoTransform = inimigoObj.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) alvoTransform = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (teleguiado && alvoTransform != null)
        {
            Vector2 direcaoAlvo = (alvoTransform.position - transform.position).normalized;
            float anguloDesejado = Mathf.Atan2(direcaoAlvo.y, direcaoAlvo.x) * Mathf.Rad2Deg;
            
            Quaternion rotacaoAtual = transform.rotation;
            Quaternion rotacaoDesejada = Quaternion.Euler(0f, 0f, anguloDesejado);
            transform.rotation = Quaternion.RotateTowards(rotacaoAtual, rotacaoDesejada, velocidadeRotacao * Time.deltaTime);

            direcaoMovimento = transform.right;
        }

        // Movimentação
        if (rb != null)
        {
            rb.linearVelocity = direcaoMovimento * velocidade;
        }
    }

    public void DefinirDirecao(Vector2 novaDirecao)
    {
        direcaoMovimento = novaDirecao.normalized;
        float angulo = Mathf.Atan2(direcaoMovimento.y, direcaoMovimento.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ProcessarColisao(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessarColisao(collision.gameObject);
    }

    // Método seguro para checar a tag "Player" (tag nativa da Unity)
    bool IsPlayer(GameObject obj)
    {
        try
        {
            return obj.CompareTag("Player") || obj.GetComponent<combate>() != null;
        }
        catch
        {
            return obj.GetComponent<combate>() != null;
        }
    }

    void ProcessarColisao(GameObject objHit)
    {
        Debug.Log("Projétil colidiu com: " + objHit.name + " | Pertence ao jogador: " + pertenceAoJogador);

        // 1. Se colidir com o Jogador
        if (IsPlayer(objHit))
        {
            bool jogadorAtacando = (Mouse.current != null && Mouse.current.leftButton.isPressed) || Input.GetMouseButton(0);

            if (rebativel && !rebatidoPeloJogador && jogadorAtacando)
            {
                RebaterProjétil();
                return;
            }

            combate playerCombate = objHit.GetComponent<combate>();
            if (playerCombate != null)
            {
                playerCombate.TomarDano(dano);
                Debug.Log("Projétil causou " + dano + " de dano ao Jogador!");
            }

            if (desapareceAoColidir)
            {
                Destroy(gameObject);
            }
        }
        // 2. Se disparado pelo jogador (ou rebatido) e colidir com Inimigo ou Boss (verificado por componentes, sem usar tags customizadas)
        else if (pertenceAoJogador && (objHit.GetComponent<inimigoScript>() != null || objHit.GetComponent<bossTutorial>() != null))
        {
            inimigoScript inimigo = objHit.GetComponent<inimigoScript>();
            if (inimigo != null)
            {
                inimigo.TomarDano(dano);
            }

            bossTutorial boss = objHit.GetComponent<bossTutorial>();
            if (boss != null)
            {
                boss.TomarDano(dano);
            }

            Debug.Log("Projétil do jogador causou dano ao Inimigo/Boss!");

            if (desapareceAoColidir)
            {
                Destroy(gameObject);
            }
        }
        // 3. Se colidir com Tilemap, paredes ou chão (verificado por componentes de cenário)
        else if (objHit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || objHit.GetComponent<CompositeCollider2D>() != null)
        {
            Debug.Log("Projétil atingiu o cenário/tilemap e foi destruído.");
            if (desapareceAoColidir)
            {
                Destroy(gameObject);
            }
        }
    }

    public void RebaterProjétil()
    {
        if (!rebativel) return;

        rebatidoPeloJogador = true;
        pertenceAoJogador = true;
        direcaoMovimento = -direcaoMovimento; // Inverte direção
        velocidade *= 1.3f; // Aumenta velocidade ao rebater

        float angulo = Mathf.Atan2(direcaoMovimento.y, direcaoMovimento.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        Debug.Log("Projétil rebatido com sucesso pelo jogador!");
    }
}
