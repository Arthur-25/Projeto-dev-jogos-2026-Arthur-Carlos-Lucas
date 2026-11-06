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
    private arma[] vetorArmas = new arma[2];
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

        // Inicialização das Armas (Espada e Lança)
        arma espada = new arma(
            1, 10f, 0.8f, 2f, 
            new Vector2(0.8f, 0.8f), new Vector2(0.6f, 1.2f), 
            0.15f, 0.5f, 10f, 10f
        );

        arma lanca = new arma(
            2, 3f, 1.5f, 4f, 
            new Vector2(0.4f, 1.2f), new Vector2(1.5f, 0.3f), 
            0.10f, 0.2f, 5f, 12f
        );

        vetorArmas[0] = espada;
        vetorArmas[1] = lanca;
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
                Application.Quit();
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

        if (textoArma != null)
        {
            textoArma.text = "Id arma atual: " + contadorArmaAtual;
        }

        if (barraPreenchimento != null && vidaMaxima > 0)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima;
        }
    }

    void ExecutarAtaque()
    {
        if (armaAtual == null) return;

        Vector3 posicaoAtaque = transform.position;
        bool atacandoParaBaixo = false;

        bool estaNoChaoRef = (scriptMovimento != null) ? scriptMovimento.EstaNoChao() : true;
        bool viradoDireitaRef = (scriptMovimento != null) ? scriptMovimento.EstaViradoDireita() : true;

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