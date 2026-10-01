using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class movimentacao : MonoBehaviour
{
    private double vida = 100;
    
    public TextMeshProUGUI textoEstilo;
    public TextMeshProUGUI textoArma;    
    public Image barraPreenchimento; 

    private float tempoDeInvencibilidade = 1.5f; 
    private float contadorInvencibilidade = 0f;

    private float velocidade = 5f;
    private bool viradoDireita = true;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb; // Adicionado para controlar a física

    private float forcaPulo = 6.7f;           
    private int limitePulos = 2;
    private int pulos = 0;

    public Transform peDoPersonagem;
    public LayerMask layerDoChao;
    public float distanciaDoRaio = 0.05f;
    private bool estaNoChao;

    public LayerMask layerInimigo;
    private bool tocaInimigo;

    // Controladores de tempo e combate locais do player
    private float contadorCooldown = 0f;         

    [Header("Ajustes Gerais")]
    public float deslocamentoY = 0f;             
    public bool mostrarHitbox = true; 
    private bool estiloCombate = true;           

    // Variável da arma atual que controla os atributos únicos
    public arma armaAtual;

    // O knockback horizontal agora é controlado dinamicamente pela arma
    private float velocidadeKnockbackX = 0f; 

    //Vetor e contador que vai controlar qual a arma atual
    private arma[] vetorArmas = new arma[2];
    private int contadorArmaAtual = 0;

    void Start(){
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>(); // Inicializa o Rigidbody2D

        // ID, ataque, alcance, velocidadeMov, hitboxPoder, hitboxPrecisao, tempoDuracaoAtaque, tempoCooldown, knockbackX, knockbackY
        arma espada = new arma(
            1,                  // ID
            10f,                // Ataque
            0.8f,               // Alcance
            2f,                 // Velocidade
            new Vector2(0.8f, 0.8f), // Hitbox Poder
            new Vector2(0.6f, 1.2f), // Hitbox Precisão
            0.15f,              // Tempo de duração do ataque
            0.5f,               // Cooldown do ataque
            10f,                // Força Knockback X
            10f                 // Força Knockback Y
        );

        arma lanca = new arma(
            2,                  // ID
            3f,                 // Ataque
            1.5f,               // Alcance
            4f,                 // Velocidade
            new Vector2(0.4f, 1.2f), // Hitbox Poder
            new Vector2(1.5f, 0.3f), // Hitbox Precisão
            0.10f,              // Tempo de duração do ataque
            0.2f,               // Cooldown do ataque
            5f,                 // Força Knockback X
            12f                 // Força Knockback Y
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

        if (Mathf.Abs(velocidadeKnockbackX) > 0f)
        {
            velocidadeKnockbackX = Mathf.MoveTowards(velocidadeKnockbackX, 0f, 25f * Time.deltaTime);
        }

        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f && armaAtual != null)
            {
                ExecutarAtaque();
                contadorCooldown = armaAtual.tempoCooldown; 
            }
        }

        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f)
            {
                estiloCombate = !estiloCombate; 
            }
        }

        estaNoChao = Physics2D.Raycast(peDoPersonagem.position, Vector2.down, distanciaDoRaio, layerDoChao);
        tocaInimigo = Physics2D.Raycast(peDoPersonagem.position, Vector2.down, distanciaDoRaio, layerInimigo);

        if (tocaInimigo && contadorInvencibilidade <= 0f)
        {
            vida -= 05; 
            print("Vida: " + vida);
            contadorInvencibilidade = tempoDeInvencibilidade;
        }

        if (barraPreenchimento != null)
        {
            barraPreenchimento.fillAmount = (float)(vida / 100.0);
        }

        if (vida <= 0){
            print("Cheguei aqui");
            Application.Quit();
        }

        if (estiloCombate){
            textoEstilo.text = "Estilo: poder";
        }else{
            textoEstilo.text = "Estilo: precisão";
        }

        textoArma.text = "Id arma atual: " + contadorArmaAtual;

        if (estaNoChao)
        {
            pulos = 0;
        }

        // Movimentação Horizontal calculada para o Rigidbody
        float velX = 0f;
        if (Mathf.Abs(velocidadeKnockbackX) > 0.1f)
        {
            velX = velocidadeKnockbackX;
        }
        else if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || 
                UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed)
            {
                viradoDireita = true;
                velX = velocidade;
            }
            else if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || 
                     UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed)
            {
                viradoDireita = false;
                velX = -velocidade;
            }
        }

        if (viradoDireita) {
            spriteRenderer.flipX = false;
        }
        else{
            spriteRenderer.flipX = true;
        }

        // Tratamento do Pulo e Gravidade via Física
        float velY = rb.linearVelocity.y;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame && (estaNoChao || pulos < limitePulos))
            {
                pulos++; 
                velY = forcaPulo;
            }
        }

        // Aplica a velocidade final diretamente no Rigidbody2D (mantendo as colisões ativas)
        rb.linearVelocity = new Vector2(velX, velY);

        // Controle da arma equipada
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame){
                if (contadorArmaAtual == vetorArmas.Length -1){
                    contadorArmaAtual = 0;
                }
                else if(vetorArmas[contadorArmaAtual + 1] == null){
                    contadorArmaAtual = 0;
                }
                else{
                    contadorArmaAtual++;
                }
                armaAtual = vetorArmas[contadorArmaAtual];
            }

            if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame){
                if (contadorArmaAtual == 0 ){
                    contadorArmaAtual = vetorArmas.Length -1;
                }else{
                    contadorArmaAtual--;
                }
                armaAtual = vetorArmas[contadorArmaAtual];
            }
        }
    }

    void ExecutarAtaque()
    {
        if (armaAtual == null) return;

        Vector3 posicaoAtaque = transform.position;
        bool atacandoParaBaixo = false;

        if (!estaNoChao && UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || 
                UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
            {
                atacandoParaBaixo = true;
            }
        }

        Vector2 tamanhoAtualDoAtaque;
        if (estiloCombate) {
            tamanhoAtualDoAtaque = armaAtual.tamanhoHitboxArmaPoder; 
        } else {
            tamanhoAtualDoAtaque = armaAtual.tamanhoHitboxArmaPrecisao; 
        }

        if (atacandoParaBaixo)
        {
            posicaoAtaque.y -= 0.6f; 
            tamanhoAtualDoAtaque = new Vector2(tamanhoAtualDoAtaque.y, tamanhoAtualDoAtaque.x);
        }
        else
        {
            posicaoAtaque.y += deslocamentoY;

            if (viradoDireita)
            {
                posicaoAtaque.x += armaAtual.alcance;
            }
            else
            {
                posicaoAtaque.x -= armaAtual.alcance;
            }
        }

        if (mostrarHitbox)
        {
            GameObject visualHitbox = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualHitbox.transform.position = posicaoAtaque;
            visualHitbox.transform.localScale = new Vector3(tamanhoAtualDoAtaque.x, tamanhoAtualDoAtaque.y, 1f); 
            
            Destroy(visualHitbox.GetComponent<Collider2D>());
            var renderer = visualHitbox.GetComponent<Renderer>();
            renderer.material.color = Color.red;

            Destroy(visualHitbox, armaAtual.tempoDuracaoAtaque);
        }

        Collider2D[] inimigosAtingidos = Physics2D.OverlapBoxAll(posicaoAtaque, tamanhoAtualDoAtaque, 0f, layerInimigo);

        if (inimigosAtingidos.Length > 0)
        {
            if (atacandoParaBaixo)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, armaAtual.forcaKnockbackY);
            }
            else
            {
                if (viradoDireita)
                {
                    velocidadeKnockbackX = -armaAtual.forcaKnockbackX; 
                }
                else
                {
                    velocidadeKnockbackX = armaAtual.forcaKnockbackX;  
                }
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
        if (peDoPersonagem != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(peDoPersonagem.position, peDoPersonagem.position + (Vector3.down * distanciaDoRaio));
        }

        if (mostrarHitbox)
        {
            Gizmos.color = Color.yellow;
            Vector3 posicaoGizmo = transform.position;
            
            posicaoGizmo.y += deslocamentoY;
            float alcanceGizmo = (armaAtual != null) ? armaAtual.alcance : 0.6f;
            posicaoGizmo.x += viradoDireita ? alcanceGizmo : -alcanceGizmo;
            
            Vector2 tamanhoGizmo = (armaAtual != null && estiloCombate) ? armaAtual.tamanhoHitboxArmaPoder : new Vector2(0.8f, 0.8f);
            Gizmos.DrawWireCube(posicaoGizmo, new Vector3(tamanhoGizmo.x, tamanhoGizmo.y, 1f));
        }
    }
}

[System.Serializable]
public class arma 
{
    public int ID;
    public float ataque;
    public float alcance;
    public float velocidade;
    public Vector2 tamanhoHitboxArmaPoder;
    public Vector2 tamanhoHitboxArmaPrecisao;

    public float tempoDuracaoAtaque;
    public float tempoCooldown;
    public float forcaKnockbackX;
    public float forcaKnockbackY;

    public arma(int id, float atq, float alc, float vel, Vector2 hitboxArmPoder, Vector2 hitboxArmPrec, float duracaoAtaque, float cooldown, float kbX, float kbY)
    {
        this.ID = id;
        this.ataque = atq;
        this.alcance = alc;
        this.velocidade = vel;
        this.tamanhoHitboxArmaPoder = hitboxArmPoder;
        this.tamanhoHitboxArmaPrecisao = hitboxArmPrec;
        this.tempoDuracaoAtaque = duracaoAtaque;
        this.tempoCooldown = cooldown;
        this.forcaKnockbackX = kbX;
        this.forcaKnockbackY = kbY;
    }
}