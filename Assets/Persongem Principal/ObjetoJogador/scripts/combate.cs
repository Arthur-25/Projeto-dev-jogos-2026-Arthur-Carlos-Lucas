using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class combate : MonoBehaviour
{
    [System.Serializable]
    public struct ConfigTimingHitbox
    {
        public string nome;
        [Min(0f)] public float delayHitbox;
        [Min(0f)] public float duracaoPosAtaque;
    }

    [Header("Estilos e armas")]
    public UnityEngine.UI.Image imagemEstiloUI;
    public Sprite spriteEstiloPoder;
    public Sprite spriteEstiloPrecisao;

    public TextMeshProUGUI textoArma;

    [Header("UI de Icone de Armas")]
    public UnityEngine.UI.Image imagemArmaIconeUI;
    public Sprite[] iconesArmas = new Sprite[4];

    [Header("Configuracao de Projetil (Revolver)")]
    public GameObject prefabProjetil;

    [Header("Ataque Supremo (Ult) e Runas de Energia")]
    [Min(1f)] public float energiaMaxima = 50f;
    [SerializeField] private float energiaInicial = 0f;
    [Min(1f)] public float custoEnergiaUlt = 50f;
    [Min(0f)] public float energiaPorAcerto = 10f;
    [SerializeField] private float cooldownUlt = 1.0f;
    [SerializeField] private float deslocamentoSpawnUlt = 1.0f;
    [SerializeField] private float deslocamentoSpawnUltY = 0.5f;
    public GameObject prefabProjetilUlt;
    public Image[] runasEnergiaUI = new Image[5];
    public Sprite[] spritesRunasCarregadas = new Sprite[5];
    public Sprite[] spritesRunasDescarregadas = new Sprite[5];

    [Header("Sistema de Vida e UI")]
    public float vidaMaxima = 100f;
    private float vida;
    private float energia;
    public Image barraPreenchimento;

    [Header("Invencibilidade")]
    private float tempoDeInvencibilidade = 1.5f;
    private float contadorInvencibilidade = 0f;

    [Header("Configuracoes de Combate")]
    public LayerMask layerInimigo;
    public float deslocamentoY = 0f;
    public bool mostrarHitbox = true;
    public bool estiloCombate = true;

    [Header("Timing das Hitboxes e Tiro")]
    [SerializeField] private ConfigTimingHitbox hitboxSword = new ConfigTimingHitbox { nome = "sword", delayHitbox = 0.08f, duracaoPosAtaque = 0f };
    [SerializeField] private ConfigTimingHitbox hitboxSpear = new ConfigTimingHitbox { nome = "spear", delayHitbox = 0.10f, duracaoPosAtaque = 0f };
    [SerializeField] private ConfigTimingHitbox hitboxAxe = new ConfigTimingHitbox { nome = "axe", delayHitbox = 0.14f, duracaoPosAtaque = 0f };
    [SerializeField] private ConfigTimingHitbox hitboxGun = new ConfigTimingHitbox { nome = "gun", delayHitbox = 0.05f, duracaoPosAtaque = 0f };
    [SerializeField] private ConfigTimingHitbox hitboxPogo = new ConfigTimingHitbox { nome = "pogo", delayHitbox = 0.06f, duracaoPosAtaque = 0f };
    [SerializeField] private ConfigTimingHitbox hitboxUlt = new ConfigTimingHitbox { nome = "ult", delayHitbox = 0.10f, duracaoPosAtaque = 0f };

    [Header("Roda de equipamento")]
    [Min(0)] public int quantidadeCura = 25;
    [SerializeField] private bool comecarComCuraCheia = true;
    [SerializeField] private Sprite iconeCura;
    [SerializeField] private Image indicadorRodaUI;
    [SerializeField] private Image iconeItemCimaUI;
    [SerializeField] private Image iconeItemBaixoUI;
    [SerializeField] private Sprite[] spritesRodaSelecao = new Sprite[4];

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
    private Animator animator;
    private bool morrendo;
    private int idAtaqueAtivo = 0;
    private Coroutine rotinaHitboxAtual;
    private Coroutine rotinaRetornoAnimacao;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        scriptMovimento = GetComponent<movimentoBasico>();
        animator = GetComponent<Animator>();

        vida = vidaMaxima;
        energia = Mathf.Clamp(energiaInicial, 0f, energiaMaxima);

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
        if (morrendo) return;

        if (contadorInvencibilidade > 0f)
        {
            contadorInvencibilidade -= Time.deltaTime;
        }

        if (contadorCooldown > 0f)
        {
            contadorCooldown -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (contadorCooldown <= 0f && PodeUsarUlt())
            {
                ExecutarUlt();
                contadorCooldown = cooldownUlt;
            }
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

    public void AdicionarEnergia(float quantidade)
    {
        energia = Mathf.Clamp(energia + quantidade, 0f, energiaMaxima);
        AtualizarRunasEnergia();
    }

    public void DefinirEnergia(float valor)
    {
        energia = Mathf.Clamp(valor, 0f, energiaMaxima);
        AtualizarRunasEnergia();
    }

    public float ObterEnergia()
    {
        return energia;
    }

    public bool PodeUsarUlt()
    {
        return energia >= custoEnergiaUlt;
    }

    private void AtualizarRunasEnergia()
    {
        if (runasEnergiaUI == null || runasEnergiaUI.Length == 0) return;

        int runasCarregadas = Mathf.Clamp(Mathf.FloorToInt(energia / 10f), 0, runasEnergiaUI.Length);

        for (int i = 0; i < runasEnergiaUI.Length; i++)
        {
            Image runa = runasEnergiaUI[i];
            if (runa == null) continue;

            bool carregada = i < runasCarregadas;
            Sprite spriteAlvo = null;

            if (carregada && spritesRunasCarregadas != null && i < spritesRunasCarregadas.Length)
            {
                spriteAlvo = spritesRunasCarregadas[i];
            }
            else if (!carregada && spritesRunasDescarregadas != null && i < spritesRunasDescarregadas.Length)
            {
                spriteAlvo = spritesRunasDescarregadas[i];
            }

            if (spriteAlvo != null)
            {
                runa.sprite = spriteAlvo;
            }
        }
    }

    private void ExecutarUlt()
    {
        if (morrendo || !PodeUsarUlt()) return;

        energia = Mathf.Max(0f, energia - custoEnergiaUlt);
        AtualizarRunasEnergia();

        idAtaqueAtivo++;
        int tokenAtual = idAtaqueAtivo;

        TocarEstadoAnimator("ult");

        if (rotinaHitboxAtual != null)
        {
            StopCoroutine(rotinaHitboxAtual);
        }

        if (rotinaRetornoAnimacao != null)
        {
            StopCoroutine(rotinaRetornoAnimacao);
        }

        rotinaHitboxAtual = StartCoroutine(DispararProjetilUltNoTiming(hitboxUlt.delayHitbox));
        rotinaRetornoAnimacao = StartCoroutine(RetornarParaIdleDepoisDoAtaque(tokenAtual, "ult", hitboxUlt.duracaoPosAtaque));
    }

    private IEnumerator DispararProjetilUltNoTiming(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (morrendo)
        {
            rotinaHitboxAtual = null;
            yield break;
        }

        bool viradoDireitaRef = (scriptMovimento != null) ? scriptMovimento.EstaViradoDireita() : true;
        Vector2 direcao = viradoDireitaRef ? Vector2.right : Vector2.left;

        Vector3 posDisparo = transform.position;
        posDisparo.x += viradoDireitaRef ? deslocamentoSpawnUlt : -deslocamentoSpawnUlt;
        posDisparo.y += deslocamentoY + deslocamentoSpawnUltY;

        if (prefabProjetilUlt != null)
        {
            GameObject projObj = Instantiate(prefabProjetilUlt, posDisparo, Quaternion.identity);
            if (projObj != null)
            {
                projetilGenerico proj = projObj.GetComponent<projetilGenerico>();
                if (proj == null)
                {
                    proj = projObj.AddComponent<projetilGenerico>();
                }
                proj.pertenceAoJogador = true;
                proj.DefinirDirecao(direcao);
            }
        }

        rotinaHitboxAtual = null;
    }

    public void TomarDano(int danoRecebido)
    {
        if (morrendo || contadorInvencibilidade > 0f) return;

        vida = Mathf.Max(0f, vida - danoRecebido);
        contadorInvencibilidade = tempoDeInvencibilidade;

        if (barraPreenchimento != null && vidaMaxima > 0f)
        {
            barraPreenchimento.fillAmount = vida / vidaMaxima;
        }

        if (vida <= 0f)
        {
            morrendo = true;
            if (rotinaHitboxAtual != null)
            {
                StopCoroutine(rotinaHitboxAtual);
                rotinaHitboxAtual = null;
            }
            if (rotinaRetornoAnimacao != null)
            {
                StopCoroutine(rotinaRetornoAnimacao);
                rotinaRetornoAnimacao = null;
            }
            StartCoroutine(RotinaMorte());
        }
    }

    private IEnumerator RotinaMorte()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        float duracao = ObterDuracaoClip("death");
        TocarEstadoAnimator("death");

        yield return new WaitForSeconds(duracao);

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

    private ConfigTimingHitbox ObterTimingHitbox(int armaId, bool pogoAtivo, out string nomeEstado)
    {
        if (pogoAtivo)
        {
            nomeEstado = "pogo";
            return hitboxPogo;
        }

        switch (armaId)
        {
            case 1:
                nomeEstado = "sword";
                return hitboxSword;
            case 2:
                nomeEstado = "spear";
                return hitboxSpear;
            case 3:
                nomeEstado = "axe";
                return hitboxAxe;
            case 4:
                nomeEstado = "gun";
                return hitboxGun;
            default:
                nomeEstado = "sword";
                return hitboxSword;
        }
    }

    private void TocarEstadoAnimator(string nomeEstado)
    {
        if (animator == null) return;
        int hashEstado = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hashEstado)) return;
        animator.Play(hashEstado, 0, 0f);
    }

    private float ObterDuracaoClip(string nomeClip)
    {
        float duracao = 0.25f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == nomeClip)
                {
                    duracao = Mathf.Max(clip.length, 0.05f);
                    break;
                }
            }
        }
        return duracao;
    }

    private IEnumerator RetornarParaIdleDepoisDoAtaque(int tokenAtaque, string nomeEstado, float duracaoPosAtaque)
    {
        float tempoEspera = duracaoPosAtaque > 0f ? duracaoPosAtaque : ObterDuracaoClip(nomeEstado);
        if (tempoEspera > 0f)
        {
            yield return new WaitForSeconds(tempoEspera);
        }

        if (!morrendo && idAtaqueAtivo == tokenAtaque)
        {
            TocarEstadoAnimator("idle");
        }

        rotinaRetornoAnimacao = null;
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
        AtualizarRunasEnergia();

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
        if (armaAtual == null || morrendo) return;

        bool estaNoChaoRef = (scriptMovimento != null) ? scriptMovimento.EstaNoChao() : true;
        bool atacandoParaBaixo = false;

        if (armaAtual.ID != 4 && !estaNoChaoRef && Keyboard.current != null)
        {
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                atacandoParaBaixo = true;
            }
        }

        idAtaqueAtivo++;
        int tokenAtual = idAtaqueAtivo;

        string nomeEstado;
        ConfigTimingHitbox timing = ObterTimingHitbox(armaAtual.ID, atacandoParaBaixo, out nomeEstado);
        TocarEstadoAnimator(nomeEstado);

        if (rotinaHitboxAtual != null)
        {
            StopCoroutine(rotinaHitboxAtual);
        }

        if (rotinaRetornoAnimacao != null)
        {
            StopCoroutine(rotinaRetornoAnimacao);
        }

        rotinaHitboxAtual = StartCoroutine(DispararHitboxNoTiming(armaAtual, atacandoParaBaixo, timing.delayHitbox));
        rotinaRetornoAnimacao = StartCoroutine(RetornarParaIdleDepoisDoAtaque(tokenAtual, nomeEstado, timing.duracaoPosAtaque));
    }

    private IEnumerator DispararHitboxNoTiming(arma armaUsada, bool atacandoParaBaixo, float delayHitbox)
    {
        if (delayHitbox > 0f)
        {
            yield return new WaitForSeconds(delayHitbox);
        }

        if (morrendo || armaUsada == null)
        {
            rotinaHitboxAtual = null;
            yield break;
        }

        bool viradoDireitaRef = (scriptMovimento != null) ? scriptMovimento.EstaViradoDireita() : true;

        if (armaUsada.ID == 4)
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
                    proj.DefinirDirecao(viradoDireitaRef ? Vector2.right : Vector2.left);
                }
            }

            rotinaHitboxAtual = null;
            yield break;
        }

        Vector3 posicaoAtaque = transform.position;
        Vector2 tamanhoAtualDoAtaque = estiloCombate ? armaUsada.tamanhoHitboxArmaPoder : armaUsada.tamanhoHitboxArmaPrecisao;

        if (atacandoParaBaixo)
        {
            posicaoAtaque.y -= 0.6f;
            tamanhoAtualDoAtaque = new Vector2(tamanhoAtualDoAtaque.y, tamanhoAtualDoAtaque.x);
        }
        else
        {
            posicaoAtaque.y += deslocamentoY;
            posicaoAtaque.x += viradoDireitaRef ? armaUsada.alcance : -armaUsada.alcance;
        }

        if (mostrarHitbox)
        {
            GameObject visualHitbox = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualHitbox.transform.position = posicaoAtaque;
            visualHitbox.transform.localScale = new Vector3(tamanhoAtualDoAtaque.x, tamanhoAtualDoAtaque.y, 1f);

            Destroy(visualHitbox.GetComponent<Collider2D>());
            visualHitbox.GetComponent<Renderer>().material.color = Color.red;
            Destroy(visualHitbox, armaUsada.tempoDuracaoAtaque);
        }

        Collider2D[] inimigosAtingidos = Physics2D.OverlapBoxAll(posicaoAtaque, tamanhoAtualDoAtaque, 0f, layerInimigo);

        if (inimigosAtingidos.Length > 0 && scriptMovimento != null && rb != null)
        {
            if (atacandoParaBaixo)
            {
                Vector2 forcaAr = new Vector2(rb.linearVelocity.x, armaUsada.forcaKnockbackY);
                scriptMovimento.AplicarForcaKnockback(forcaAr, 0.15f);
            }
            else
            {
                float knockX = viradoDireitaRef ? -armaUsada.forcaKnockbackX : armaUsada.forcaKnockbackX;
                Vector2 forcaHorizontal = new Vector2(knockX, rb.linearVelocity.y);
                scriptMovimento.AplicarForcaKnockback(forcaHorizontal, 0.15f);
            }
        }

        bool acertouAlguem = false;
        foreach (Collider2D colisorInimigo in inimigosAtingidos)
        {
            inimigoScript inimigo = colisorInimigo.GetComponent<inimigoScript>();
            if (inimigo != null)
            {
                inimigo.TomarDano((int)armaUsada.ataque);
                acertouAlguem = true;
            }
        }

        if (acertouAlguem && energiaPorAcerto > 0f)
        {
            AdicionarEnergia(energiaPorAcerto);
        }

        rotinaHitboxAtual = null;
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