using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class gerenciadorGameOver : MonoBehaviour
{
    private void Awake()
    {
        ConfigurarBotao("botaoReviver", ReviverDoCheckpoint);
        ConfigurarBotao("botaoMenu", VoltarParaMenuPrincipal);
    }

    // Redireciona para o carregamento do checkpoint caso esteja vinculado
    public void VoltarParaTutorial()
    {
        ReviverDoCheckpoint();
    }

    // Redireciona para o carregamento do checkpoint caso esteja vinculado
    public void CarregarCena(string nomeCena)
    {
        GerenciadorCheckpoint.CarregarCheckpointOuCenaInicial(nomeCena);
    }

    // Método principal para reviver do último checkpoint salvo
    public void ReviverDoCheckpoint()
    {
        Debug.Log(">>> [GAME OVER] Botão ReviverDoCheckpoint foi acionado!");
        GerenciadorCheckpoint.CarregarCheckpointOuCenaInicial("Tutorial");
    }

    public void VoltarParaMenuPrincipal()
    {
        Time.timeScale = 1f;
        if (!Application.CanStreamedLevelBeLoaded("menuInicial"))
        {
            Debug.LogError("A cena 'menuInicial' nao esta incluida no Build Profile ativo.", this);
            return;
        }

        SceneManager.LoadScene("menuInicial");
    }

    private void ConfigurarBotao(string nome, UnityEngine.Events.UnityAction acao)
    {
        Transform alvo = transform.Find(nome);
        if (alvo == null)
        {
            Debug.LogWarning($"{name}: botao '{nome}' nao foi encontrado no Canvas.", this);
            return;
        }

        Button botao = alvo.GetComponent<Button>();
        if (botao == null) botao = alvo.gameObject.AddComponent<Button>();

        Graphic grafico = alvo.GetComponent<Graphic>();
        if (grafico != null) botao.targetGraphic = grafico;

        if (botao.onClick.GetPersistentEventCount() == 0)
        {
            botao.onClick.AddListener(acao);
        }
    }
}
