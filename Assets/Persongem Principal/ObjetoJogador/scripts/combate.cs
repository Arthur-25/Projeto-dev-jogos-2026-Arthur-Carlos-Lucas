using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class combate : MonoBehaviour
{
    [Header("Estilos e armas")]
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

    [Header("Roda de equipamento")]
    [Min(0)] public int quantidadeCura = 25;
    [SerializeField] private bool comecarComCuraCheia = true;
    [SerializeField] private Sprite iconeCura;
    [SerializeField] private Image indicadorRodaUI;
    [SerializeField] private Image iconeItemCimaUI;
    [SerializeField] private Image iconeItemBaixoUI;
    [SerializeField] private Sprite[] spritesRodaSelecao = new Sprite[4]; // 0=Cima, 1=Direita, 2=Baixo, 3=Esquerda

    [HideInInspector] public arma armaAtual;

    private float contadorCooldown = 0f;
    private arma[] vetorArmas = new arma[4];
    private arma[] armasPorSlot = new arma[4];
    private bool[] curaDisponivelPorSlot = new bool[4];
    private int slotEquipado = 3;
    private Image iconeArmaEsquerdaUI;
    private Image iconeArmaDireitaUI;

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

        arma espada = new arma(1, 3f, 1.2f, 2f, new Vector2(0.8f, 0.8f), new Vector2(0.6f, 1.6f), 0.15f, 0.3f, 5f, 4f);
        arma lanca = new arma(2, 1f, 2.5f, 4f, new Vector2(2f, 1.2f), new Vector2(3f, 0.6f), 0.10f, 0.8f, 3f, 7f);
        arma machado = new arma(3, 20f, 1.5f, 1.5f, new Vector2(3f, 3f), new Vector2(5.0f, 0.8f), 0.20f, 3f, 18f, 6f);
        arma revolver = new arma(4, 1.5f, 5.0f, 8f, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0.10f, 1f, 2f, 2f);

        vetorArmas[0] = espada;
        vetorArmas[1] = lanca;
        vetorArmas[2] = machado;
        vetorArmas[3] = revolver;

        int idEsquerda = PlayerPrefs.GetInt("ArmaEsquerdaId", 1);
        int idDireita = PlayerPrefs.GetInt("ArmaDireitaId", 2);
        armasPorSlot[3] = ObterArmaPorID(idEsquerda);
        armasPorSlot[1] = ObterArmaPorID(idDireita);

        if (comecarComCuraCheia)
        {
            curaDisponivelPorSlot[0] = true;
            curaDisponivelPorSlot[2] = true;
            PlayerPrefs.SetInt("ItemCimaCuraDisponivel", 1);
            PlayerPrefs.SetInt("ItemBaixoCuraDisponivel", 1);
            PlayerPrefs.Save();
        }
        else
        {
            curaDisponivelPorSlot[0] = PlayerPrefs.GetInt("ItemCimaCuraDisponivel", 1) == 1;
            curaDisponivelPorSlot[2] = PlayerPrefs.GetInt("ItemBaixoCuraDisponivel", 1) == 1;
        }

        iconeArmaEsquerdaUI = EncontrarImagemUI("armaEsquerda");
        iconeArmaDireitaUI = EncontrarImagemUI("armaDireita");

        if (iconeItemCimaUI == null)
        {
            iconeItemCimaUI = EncontrarImagemUI("itemCima");
        }

        if (iconeItemBaixoUI == null)
        {
            iconeItemBaixoUI = EncontrarImagemUI("itemBaixo");
        }

        if (indicadorRodaUI == null)
        {
            indicadorRodaUI = EncontrarImagemUI("rodaItens");
        }

        slotEquipado = PlayerPrefs.GetInt("SlotRodaAtual", 3);
        if (!SlotDisponivel(slotEquipado))
        {
            slotEquipado = EncontrarProximoSlot(slotEquipado, 1);
        }

        AtualizarArmaEquipada();
        AtualizarUI();
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

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f)
            {
                ExecutarAcaoDoSlot();
            }
        }

        AtualizarSelecaoPeloScroll();

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f)
            {
                estiloCombate = !estiloCombate;
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
                string cenaAtual = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string cenaGameOver = cenaAtual == "Tutorial" ? "gameOverSombra" : "gameOverNormal";

                if (Application.CanStreamedLevelBeLoaded(cenaGameOver))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(cenaGameOver);
                }
                else
                {
                    Debug.LogError($"A cena '{cenaGameOver}' nao esta no Build Profile ativo. Adicione-a em File -> Build Profiles.");
                }
            }
        }
    }

    void AtualizarUI()
    {
        if (imagemEstiloUI != null)
        {
            imagemEstiloUI.sprite = estiloCombate ? spriteEstiloPoder : spriteEstiloPrecisao;
        }

        if (textoArma != null)
        {
            textoArma.text = armaAtual != null ? "ID arma atual: " + armaAtual.ID : "Sem arma equipada";
        }

        AtualizarIconeArma(iconeArmaEsquerdaUI, armasPorSlot[3]);
        AtualizarIconeArma(iconeArmaDireitaUI, armasPorSlot[1]);
        AtualizarIconeCura(iconeItemCimaUI, 0);
        AtualizarIconeCura(iconeItemBaixoUI, 2);

        if (imagemArmaIconeUI != null && armaAtual != null && iconesArmas != null &&
            armaAtual.ID > 0 && armaAtual.ID <= iconesArmas.Length && iconesArmas[armaAtual.ID - 1] != null)
        {
            imagemArmaIconeUI.sprite = iconesArmas[armaAtual.ID - 1];
            imagemArmaIconeUI.gameObject.SetActive(true);
        }
        else if (imagemArmaIconeUI != null)
        {
            imagemArmaIconeUI.gameObject.SetActive(false);
        }

        AtualizarSpriteRoda();

        if (barraPreenchimento != null && vidaMaxima > 0)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima;
        }
    }

    private void AtualizarSpriteRoda()
    {
        if (indicadorRodaUI == null || spritesRodaSelecao == null) return;
        if (slotEquipado < 0 || slotEquipado >= spritesRodaSelecao.Length) return;

        Sprite spriteAlvo = spritesRodaSelecao[slotEquipado];
        if (spriteAlvo != null)
        {
            indicadorRodaUI.enabled = true;
            indicadorRodaUI.color = Color.white;
            indicadorRodaUI.sprite = spriteAlvo;
        }
    }

    private arma ObterArmaPorID(int id)
    {
        return id >= 1 && id <= vetorArmas.Length ? vetorArmas[id - 1] : null;
    }

    private void ExecutarAcaoDoSlot()
    {
        if (slotEquipado == 0 || slotEquipado == 2)
        {
            ConsumirCura(slotEquipado);
            return;
        }

        if (armaAtual == null) return;
        ExecutarAtaque();
        contadorCooldown = armaAtual.tempoCooldown;
    }

    private void ConsumirCura(int slot)
    {
        if (slot != 0 && slot != 2) return;
        if (!curaDisponivelPorSlot[slot]) return;

        vida = Mathf.Min(vidaMaxima, vida + quantidadeCura);
        curaDisponivelPorSlot[slot] = false;

        PlayerPrefs.SetInt(slot == 0 ? "ItemCimaCuraDisponivel" : "ItemBaixoCuraDisponivel", 0);
        PlayerPrefs.Save();

        if (barraPreenchimento != null && vidaMaxima > 0)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima;
        }

        contadorCooldown = 0.25f;

        slotEquipado = EncontrarProximoSlot(slot, 1);
        AtualizarArmaEquipada();
        AtualizarUI();
    }

    private void AtualizarSelecaoPeloScroll()
    {
        if (Mouse.current == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < 0.01f) return;

        int sentido = scrollY > 0f ? 1 : -1;
        slotEquipado = EncontrarProximoSlot(slotEquipado, sentido);
        AtualizarArmaEquipada();
        AtualizarUI();
    }

    private int EncontrarProximoSlot(int origem, int sentido)
    {
        int candidato = origem;
        for (int tentativa = 0; tentativa < 4; tentativa++)
        {
            candidato = (candidato + sentido + 4) % 4;
            if (SlotDisponivel(candidato)) return candidato;
        }

        return origem;
    }

    private bool SlotDisponivel(int slot)
    {
        if (slot == 0 || slot == 2) return curaDisponivelPorSlot[slot];
        if (slot == 1 || slot == 3) return armasPorSlot[slot] != null;
        return false;
    }

    private void AtualizarArmaEquipada()
    {
        armaAtual = (slotEquipado == 1 || slotEquipado == 3) ? armasPorSlot[slotEquipado] : null;
        PlayerPrefs.SetInt("SlotRodaAtual", slotEquipado);
        PlayerPrefs.Save();
    }

    private void AtualizarIconeArma(Image imagem, arma armaSlot)
    {
        if (imagem == null) return;
        bool visivel = armaSlot != null && iconesArmas != null && armaSlot.ID > 0 &&
                       armaSlot.ID <= iconesArmas.Length && iconesArmas[armaSlot.ID - 1] != null;
        imagem.enabled = visivel;
        if (visivel) imagem.sprite = iconesArmas[armaSlot.ID - 1];
    }

    private void AtualizarIconeCura(Image imagem, int slot)
    {
        if (imagem == null) return;
        bool visivel = curaDisponivelPorSlot[slot] && iconeCura != null;
        imagem.gameObject.SetActive(visivel);
        imagem.enabled = visivel;
        if (visivel)
        {
            imagem.color = Color.white;
            imagem.sprite = iconeCura;
        }
    }

    private static Image EncontrarImagemUI(string nome)
    {
        GameObject objeto = GameObject.Find(nome);
        return objeto != null ? objeto.GetComponent<Image>() : null;
    }

    void ExecutarAtaque()
    {
        if (armaAtual == null) return;

        bool estaNoChaoRef = (scriptMovimento != null) ? scriptMovimento.EstaNoChao() : true;
        bool viradoDireitaRef = (scriptMovimento != null) ? scriptMovimento.EstaViradoDireita() : true;

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