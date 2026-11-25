using UnityEngine.SceneManagement;
using UnityEngine;

public class scriptMenuJogar : MonoBehaviour
{
    [SerializeField] private string cenaSemCheckpoint = "Tutorial";

    public void IniciarJogo()
    {
        if (!Application.CanStreamedLevelBeLoaded("selecaoArmas"))
        {
            Debug.LogError("A cena 'selecaoArmas' nao esta incluida no Build Profile ativo.", this);
            return;
        }

        SceneManager.LoadScene("selecaoArmas");
    }
}
