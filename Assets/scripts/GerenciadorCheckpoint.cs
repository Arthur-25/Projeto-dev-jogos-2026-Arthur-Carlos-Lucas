using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class GerenciadorCheckpoint
{
    private static string CaminhoArquivo => Application.persistentDataPath + "/checkpoint.txt";

    [System.Serializable]
    private class DadosCheckpoint
    {
        public string nomeCena;
        public float posX;
        public float posY;
        public float posZ;
    }

    public static void Salvar(string nomeCena, Vector3 posicao)
    {
        DadosCheckpoint dados = new DadosCheckpoint
        {
            nomeCena = nomeCena,
            posX = posicao.x,
            posY = posicao.y,
            posZ = posicao.z
        };

        string json = JsonUtility.ToJson(dados);
        File.WriteAllText(CaminhoArquivo, json);
        Debug.Log(">>> [CHECKPOINT] Salvo com sucesso! Cena: " + nomeCena + " | Posição: " + posicao + " | Arquivo: " + CaminhoArquivo);
    }

    public static bool TemCheckpointSalvo()
    {
        bool existe = File.Exists(CaminhoArquivo);
        Debug.Log(">>> [CHECKPOINT] TemCheckpointSalvo: " + existe + " (Caminho: " + CaminhoArquivo + ")");
        return existe;
    }

    public static void CarregarCheckpointOuCenaInicial(string cenaSemCheckpoint)
    {
        DadosCheckpoint dados = null;

        if (TemCheckpointSalvo())
        {
            try
            {
                string json = File.ReadAllText(CaminhoArquivo);
                dados = JsonUtility.FromJson<DadosCheckpoint>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(">>> [CHECKPOINT] O save nao pode ser lido: " + e.Message);
            }
        }

        if (dados != null && !string.IsNullOrWhiteSpace(dados.nomeCena) &&
            Application.CanStreamedLevelBeLoaded(dados.nomeCena))
        {
            PlayerPrefs.SetFloat("RespawnX", dados.posX);
            PlayerPrefs.SetFloat("RespawnY", dados.posY);
            PlayerPrefs.SetFloat("RespawnZ", dados.posZ);
            PlayerPrefs.SetInt("TemRespawnPendente", 1);
            PlayerPrefs.Save();
            Debug.Log(">>> [CHECKPOINT] Retomando cena salva: " + dados.nomeCena);
            SceneManager.LoadScene(dados.nomeCena);
            return;
        }

        PlayerPrefs.SetInt("TemRespawnPendente", 0);
        PlayerPrefs.Save();

        if (string.IsNullOrWhiteSpace(cenaSemCheckpoint) ||
            !Application.CanStreamedLevelBeLoaded(cenaSemCheckpoint))
        {
            Debug.LogError(">>> [CHECKPOINT] A cena fallback nao esta disponivel: " + cenaSemCheckpoint);
            return;
        }

        Debug.Log(">>> [CHECKPOINT] Sem save valido. Iniciando cena: " + cenaSemCheckpoint);
        SceneManager.LoadScene(cenaSemCheckpoint);
    }

    public static void CarregarJogo()
    {
        Debug.Log(">>> [CHECKPOINT] CarregarJogo() foi chamado.");
        CarregarCheckpointOuCenaInicial("Tutorial");
    }

    public static Vector3? ObterPosicaoPendente()
    {
        int pendente = PlayerPrefs.GetInt("TemRespawnPendente", 0);
        Debug.Log(">>> [CHECKPOINT] ObterPosicaoPendente - TemRespawnPendente flag: " + pendente);

        if (pendente == 1)
        {
            PlayerPrefs.SetInt("TemRespawnPendente", 0);
            PlayerPrefs.Save();
            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat("RespawnX", 0f),
                PlayerPrefs.GetFloat("RespawnY", 0f),
                PlayerPrefs.GetFloat("RespawnZ", 0f)
            );
            Debug.Log(">>> [CHECKPOINT] Posição pendente obtida com sucesso: " + pos);
            return pos;
        }
        return null;
    }
}
