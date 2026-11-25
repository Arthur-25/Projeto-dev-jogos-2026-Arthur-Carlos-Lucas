using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class scriptPauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject painelConteudo;
    [SerializeField] private string cenaMenuInicial = "menuInicial";
    [SerializeField] private string cenaSemCheckpoint = "Tutorial";

    private bool pausado;

    void Awake()
    {
        Time.timeScale = 1f;

        if (painelConteudo == null)
        {
            Transform canvas = transform.Find("Canvas");
            if (canvas != null) painelConteudo = canvas.gameObject;
        }

        Image imagemFundo = GetComponent<Image>();
        if (imagemFundo != null) imagemFundo.raycastTarget = false;

        if (painelConteudo != null)
        {
            painelConteudo.transform.localScale = Vector3.one;
            painelConteudo.SetActive(false);
            ConfigurarBotao("Canvas/menuFrame/ContinueON", Continuar);
            ConfigurarBotao("Canvas/menuFrame/ReiniciarON", ReiniciarCheckpoint);
            ConfigurarBotao("Canvas/menuFrame/SairON", SairParaMenu);
        }
        else
        {
            Debug.LogError($"{name}: nao encontrei o filho Canvas do PauseMenuPanel.", this);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            AlternarPausa();
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void AlternarPausa()
    {
        DefinirPausa(!pausado);
    }

    public void Continuar()
    {
        DefinirPausa(false);
    }

    public void ReiniciarCheckpoint()
    {
        Time.timeScale = 1f;
        pausado = false;
        GerenciadorCheckpoint.CarregarCheckpointOuCenaInicial(cenaSemCheckpoint);
    }

    public void SairParaMenu()
    {
        Time.timeScale = 1f;
        pausado = false;

        if (!Application.CanStreamedLevelBeLoaded(cenaMenuInicial))
        {
            Debug.LogError($"A cena '{cenaMenuInicial}' nao esta incluida no Build Profile ativo.", this);
            return;
        }

        SceneManager.LoadScene(cenaMenuInicial);
    }

    void DefinirPausa(bool pausar)
    {
        pausado = pausar;
        Time.timeScale = pausado ? 0f : 1f;

        if (painelConteudo != null)
        {
            painelConteudo.transform.localScale = Vector3.one;
            painelConteudo.SetActive(pausado);
        }
    }

    void ConfigurarBotao(string caminho, UnityEngine.Events.UnityAction acao)
    {
        Transform alvo = transform.Find(caminho);
        if (alvo == null)
        {
            Debug.LogWarning($"{name}: nao encontrei o controle '{caminho}'.", this);
            return;
        }

        Button botao = alvo.GetComponent<Button>();
        if (botao == null) botao = alvo.gameObject.AddComponent<Button>();

        Graphic grafico = alvo.GetComponent<Graphic>();
        if (grafico != null) botao.targetGraphic = grafico;
        botao.onClick.RemoveListener(acao);
        botao.onClick.AddListener(acao);
    }
}
