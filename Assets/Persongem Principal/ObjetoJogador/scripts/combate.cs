using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class combate : MonoBehaviour
{
    [Header("Estilos e armas")]
    // Campos atualizados para receber a Image e os Sprites no Inspector
    public UnityEngine.UI.Image imagemEstiloUI; 
    public Sprite spriteEstiloPoder;
    public Sprite spriteEstiloPrecisao;

    public TextMeshProUGUI textoArma;   

    [Header("UI de Ícone de Armas")]
    public UnityEngine.UI.Image imagemArmaIconeUI;
    public Sprite[] iconesArmas = new Sprite[4];

    [Header("Configuração de Projétil (Revólver)")]
    public GameObject prefabProjetil;

    [Header("Sistema de Vida e UI")]
    public float vidaMaxima = 100f;
    private float vida;
    public Image barraPreenchimento; 

    [Header("Invencibilidade")]
    private float tempoDeInvencibilidade = 1.5f; 
    private float contadorInvencibilidade = 0f;

    [Header("Configurações de Combate")]
    public LayerMask layerInimigo;
    public float deslocamentoY = 0f;             
    public bool mostrarHitbox = true; 
    public bool estiloCombate = true; // true = Poder, false = Precisão

    [HideInInspector] public arma armaAtual;

    private float contadorCooldown = 0f;         
    private arma[] vetorArmas = new arma[4];
    private int contadorArmaAtual = 0;

    private Rigidbody2D rb;
    private movimentoBasico scriptMovimento;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        scriptMovimento = GetComponent<movimentoBasico>();

        vida = vidaMaxima;

        if (barraPreenchimento != null)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima; 
        }

        // Inicialização das Armas (Espada, Lança, Machado e Revólver)
        /* Ordem dos parâmetros do construtor da arma:
         * new arma(
         *     int ID,                  // 1 = Espada, 2 = Lança, 3 = Machado, 4 = Revólver (Projétil)
         *     float ataque,            // Dano causado
         *     float alcance,           // Distância do ataque em relação ao jogador
         *     float velocidade,        // Velocidade da arma
         *     Vector2 hitboxPoder,     // Tamanho da hitbox no Estilo Poder (Largura, Altura)
         *     Vector2 hitboxPrecisao,  // Tamanho da hitbox no Estilo Precisão (Largura, Altura)
         *     float tempoDuracaoAtaque,// Tempo que a hitbox fica ativa (segundos)
         *     float tempoCooldown,     // Tempo de espera entre os ataques (segundos)
         *     float kbX,               // Força de empurrão horizontal (Knockback X)
         *     float kbY                // Força de empurrão vertical (Knockback Y)
         * );
         */

        arma espada = new arma(
            1,                  // ID
            3f,                // Ataque
            1.2f,               // Alcance
            2f,                 // Velocidade
            new Vector2(0.8f, 0.8f),   // Hitbox Poder (Largura, Altura)
            new Vector2(0.6f, 1.6f),   // Hitbox Precisão (Largura, Altura)
            0.15f,              // Duração do Ataque
            0.3f,               // Cooldown
            5f,                // Knockback X
            4f                 // Knockback Y
        );

        arma lanca = new arma(
            2,                  // ID
            1f,                 // Ataque
            2.5f,               // Alcance
            4f,                 // Velocidade
            new Vector2(2f, 1.2f),   // Hitbox Poder
            new Vector2(3f, 0.6f),   // Hitbox Precisão
            0.10f,              // Duração do Ataque
            0.8f,               // Cooldown
            3f,                 // Knockback X
            7f                 // Knockback Y
        );

        arma machado = new arma(
            3,                  // ID
            20f,                // Ataque
            1.5f,               // Alcance
            1.5f,               // Velocidade
            new Vector2(3f, 3f),   // Hitbox Poder
            new Vector2(5.0f, 0.8f),   // Hitbox Precisão
            0.20f,              // Duração do Ataque
            3f,               // Cooldown
            18f,                // Knockback X
            6f                  // Knockback Y
        );

        arma revolver = new arma(
            4,                  // ID (4 dispara projétil)
            1.5f,                // Ataque
            5.0f,               // Alcance / Distância
            8f,                 // Velocidade do projétil
            new Vector2(0.5f, 0.5f),   // Hitbox (não usada para projétil)
            new Vector2(0.5f, 0.5f),   // Hitbox (não usada para projétil)
            0.10f,              // Duração do Ataque
            1f,               // Cooldown (cadência de tiro)
            2f,                 // Knockback X
            2f                  // Knockback Y
        );

        vetorArmas[0] = espada;
        vetorArmas[1] = lanca;
        vetorArmas[2] = machado;
        vetorArmas[3] = revolver;
        armaAtual = vetorArmas[0];
    }

    void Update()
    {
        if (contadorInvencibilidade > 0f)
        {
            contadorInvencibilidade -= Time.deltaTime;
        }

        if (contadorCooldown > 0f)
        {
            contadorCooldown -= Time.deltaTime;
        }

        // Input para Atacar
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f && armaAtual != null)
            {
                ExecutarAtaque();
                contadorCooldown = armaAtual.tempoCooldown; 
            }
        }

        // Input para alternar Estilo de Combate
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f)
            {
                estiloCombate = !estiloCombate; 
            }
        }

        // Input para alternar Armas
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                contadorArmaAtual = (contadorArmaAtual + 1) % vetorArmas.Length;
                armaAtual = vetorArmas[contadorArmaAtual];
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                contadorArmaAtual = (contadorArmaAtual - 1 + vetorArmas.Length) % vetorArmas.Length;
                armaAtual = vetorArmas[contadorArmaAtual];
            }
        }

        AtualizarUI();
    }

    public void TomarDano(int danoRecebido)
    {
        if (contadorInvencibilidade <= 0f)
        {
            vida -= danoRecebido;
            contadorInvencibilidade = tempoDeInvencibilidade;

            if (barraPreenchimento != null)
            {
                barraPreenchimento.fillAmount = vida / vidaMaxima; 
            }

            if (vida <= 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("gameOverSombra");
            }
        }
    }

    void AtualizarUI()
    {
        // Troca dinamicamente o Sprite da imagem com base no estilo atual
        if (imagemEstiloUI != null)
        {
            if (estiloCombate)
            {
                imagemEstiloUI.sprite = spriteEstiloPoder;
            }
            else
            {
                imagemEstiloUI.sprite = spriteEstiloPrecisao;
            }
        }

        // Atualiza o ícone da arma atual na UI (sem exibir texto)
        if (imagemArmaIconeUI != null && iconesArmas != null && contadorArmaAtual < iconesArmas.Length && iconesArmas[contadorArmaAtual] != null)
        {
            imagemArmaIconeUI.sprite = iconesArmas[contadorArmaAtual];
            imagemArmaIconeUI.gameObject.SetActive(true);
        }

        if (barraPreenchimento != null && vidaMaxima > 0)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima;
        }
    }

    void ExecutarAtaque()
    {
        if (armaAtual == null) return;

        bool estaNoChaoRef = (scriptMovimento != null) ? scriptMovimento.EstaNoChao() : true;
        bool viradoDireitaRef = (scriptMovimento != null) ? scriptMovimento.EstaViradoDireita() : true;

        // Se a arma atual for o Revólver (ID = 4), disparar projétil
        if (armaAtual.ID == 4)
        {
            Vector3 posDisparo = transform.position;
            posDisparo.x += viradoDireitaRef ? 0.8f : -0.8f;
            posDisparo.y += deslocamentoY + 0.2f;

            if (prefabProjetil != null)
            {
                GameObject projObj = Instantiate(prefabProjetil, posDisparo, Quaternion.identity);
                if (projObj != null)
                {
                    projetilGenerico proj = projObj.GetComponent<projetilGenerico>();
                    if (proj == null)
                    {
                        proj = projObj.AddComponent<projetilGenerico>();
                    }
                    proj.pertenceAoJogador = true;
                    proj.dano = (int)armaAtual.ataque;
                    proj.DefinirDirecao(viradoDireitaRef ? Vector2.right : Vector2.left);
                }
            }
            else
            {
                // Fallback robusto programático com Sprite visível se nenhum prefab foi atribuído no Inspector
                GameObject projObj = new GameObject("ProjetilRevolver_Fallback");
                projObj.transform.position = posDisparo;
                projObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

                SpriteRenderer sr = projObj.AddComponent<SpriteRenderer>();
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.yellow);
                texture.Apply();
                sr.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

                BoxCollider2D col = projObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.8f, 0.8f);

                Rigidbody2D rbProj = projObj.AddComponent<Rigidbody2D>();
                rbProj.gravityScale = 0f;

                projetilGenerico proj = projObj.AddComponent<projetilGenerico>();
                proj.pertenceAoJogador = true;
                proj.dano = (int)armaAtual.ataque;
                proj.velocidade = 10f;
                proj.DefinirDirecao(viradoDireitaRef ? Vector2.right : Vector2.left);
            }
            return;
        }

        Vector3 posicaoAtaque = transform.position;
        bool atacandoParaBaixo = false;

        if (!estaNoChaoRef && Keyboard.current != null)
        {
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                atacandoParaBaixo = true;
            }
        }

        Vector2 tamanhoAtualDoAtaque = estiloCombate ? armaAtual.tamanhoHitboxArmaPoder : armaAtual.tamanhoHitboxArmaPrecisao; 

        if (atacandoParaBaixo)
        {
            posicaoAtaque.y -= 0.6f; 
            tamanhoAtualDoAtaque = new Vector2(tamanhoAtualDoAtaque.y, tamanhoAtualDoAtaque.x);
        }
        else
        {
            posicaoAtaque.y += deslocamentoY;
            posicaoAtaque.x += viradoDireitaRef ? armaAtual.alcance : -armaAtual.alcance;
        }

        if (mostrarHitbox)
        {
            GameObject visualHitbox = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualHitbox.transform.position = posicaoAtaque;
            visualHitbox.transform.localScale = new Vector3(tamanhoAtualDoAtaque.x, tamanhoAtualDoAtaque.y, 1f); 
            
            Destroy(visualHitbox.GetComponent<Collider2D>());
            visualHitbox.GetComponent<Renderer>().material.color = Color.red;
            Destroy(visualHitbox, armaAtual.tempoDuracaoAtaque);
        }

        Collider2D[] inimigosAtingidos = Physics2D.OverlapBoxAll(posicaoAtaque, tamanhoAtualDoAtaque, 0f, layerInimigo);

        if (inimigosAtingidos.Length > 0 && scriptMovimento != null)
        {
            if (atacandoParaBaixo)
            {
                Vector2 forcaAr = new Vector2(rb.linearVelocity.x, armaAtual.forcaKnockbackY);
                scriptMovimento.AplicarForcaKnockback(forcaAr, 0.15f);
            }
            else
            {
                float knockX = viradoDireitaRef ? -armaAtual.forcaKnockbackX : armaAtual.forcaKnockbackX;
                Vector2 forcaHorizontal = new Vector2(knockX, rb.linearVelocity.y);
                scriptMovimento.AplicarForcaKnockback(forcaHorizontal, 0.15f);
            }
        }

        foreach (Collider2D colisorInimigo in inimigosAtingidos)
        {
            inimigoScript inimigo = colisorInimigo.GetComponent<inimigoScript>();
            if (inimigo != null)
            {
                inimigo.TomarDano((int)armaAtual.ataque);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (mostrarHitbox)
        {
            Gizmos.color = Color.yellow;
            Vector3 posicaoGizmo = transform.position;
            posicaoGizmo.y += deslocamentoY;
            float alcanceGizmo = (armaAtual != null) ? armaAtual.alcance : 0.6f;
            
            bool dir = true;
            var mov = GetComponent<movimentoBasico>();
            if (mov != null) dir = mov.EstaViradoDireita();

            posicaoGizmo.x += dir ? alcanceGizmo : -alcanceGizmo;
            
            Vector2 tamanhoGizmo = (armaAtual != null && estiloCombate) ? armaAtual.tamanhoHitboxArmaPoder : new Vector2(0.8f, 0.8f);
            Gizmos.DrawWireCube(posicaoGizmo, new Vector3(tamanhoGizmo.x, tamanhoGizmo.y, 1f));
        }
    }
}