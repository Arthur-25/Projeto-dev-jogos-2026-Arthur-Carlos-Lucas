using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.IO;

public class scriptMenuJogar : MonoBehaviour
{
    [SerializeField] private AudioClip somApagarSave;
    [SerializeField] [Range(0f, 1f)] private float volumeSom = 1f;

    void Update()
    {
        bool apertouDelete = (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
                             || Input.GetKeyDown(KeyCode.Delete);

        if (apertouDelete)
        {
            ResetarSave();
        }
    }

    public void IniciarJogo()
    {
        if (!Application.CanStreamedLevelBeLoaded("selecaoArmas"))
        {
            Debug.LogError("A cena 'selecaoArmas' nao esta incluida no Build Profile ativo.", this);
            return;
        }

        SceneManager.LoadScene("selecaoArmas");
    }

    public void ResetarSave()
    {
        string caminhoCheckpoint = Application.persistentDataPath + "/checkpoint.txt";
        if (File.Exists(caminhoCheckpoint))
        {
            File.Delete(caminhoCheckpoint);
        }

        PlayerPrefs.DeleteKey("TemRespawnPendente");
        PlayerPrefs.DeleteKey("RespawnX");
        PlayerPrefs.DeleteKey("RespawnY");
        PlayerPrefs.DeleteKey("RespawnZ");
        PlayerPrefs.DeleteKey("ArmaEsquerdaId");
        PlayerPrefs.DeleteKey("ArmaDireitaId");
        PlayerPrefs.DeleteKey("ItemCimaCuraDisponivel");
        PlayerPrefs.DeleteKey("ItemBaixoCuraDisponivel");
        PlayerPrefs.DeleteKey("SlotRodaAtual");
        PlayerPrefs.Save();

        TocarSomConfirmacao();
    }

    private void TocarSomConfirmacao()
    {
        if (somApagarSave == null) return;

        GameObject objSom = new GameObject("SomResetSave");
        AudioSource fonte = objSom.AddComponent<AudioSource>();
        fonte.clip = somApagarSave;
        fonte.volume = volumeSom;
        fonte.Play();

        Destroy(objSom, somApagarSave.length + 0.1f);
    }
}