using UnityEngine;
using UnityEngine.SceneManagement;

public class gerenciadorGameOver : MonoBehaviour
{
    // Redireciona para o carregamento do checkpoint caso esteja vinculado
    public void VoltarParaTutorial()
    {
        ReviverDoCheckpoint();
    }

    // Redireciona para o carregamento do checkpoint caso esteja vinculado
    public void CarregarCena(string nomeCena)
    {
        ReviverDoCheckpoint();
    }

    // Método principal para reviver do último checkpoint salvo
    public void ReviverDoCheckpoint()
    {
        Debug.Log(">>> [GAME OVER] Botão ReviverDoCheckpoint foi acionado!");
        if (GerenciadorCheckpoint.TemCheckpointSalvo())
        {
            Debug.Log(">>> [GAME OVER] Checkpoint encontrado, chamando GerenciadorCheckpoint.CarregarJogo()...");
            GerenciadorCheckpoint.CarregarJogo();
        }
        else
        {
            Debug.LogWarning(">>> [GAME OVER] Nenhum checkpoint salvo encontrado! Carregando Tutorial.");
            SceneManager.LoadScene("Tutorial");
        }
    }
}
