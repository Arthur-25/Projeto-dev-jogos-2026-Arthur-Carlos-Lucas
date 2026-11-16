using UnityEngine;
using UnityEngine.SceneManagement;

public class gerenciadorGameOver : MonoBehaviour
{
    // Método para ser chamado pelo botão na cena gameOverSombra
    public void VoltarParaTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }

    // Método genérico caso queira carregar outra cena pelo nome
    public void CarregarCena(string nomeCena)
    {
        SceneManager.LoadScene(nomeCena);
    }
}
