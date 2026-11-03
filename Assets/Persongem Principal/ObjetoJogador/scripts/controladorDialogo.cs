using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class controladorDialogo : MonoBehaviour
{

    [Header("Interface")]
    public GameObject painelDialogo; // Arraste o GameObject da sua caixa de diálogo aqui no Inspetor
    public TextMeshProUGUI textoCaixa;
    public TextMeshProUGUI textoLocutor;
    public objetoDialogo[] vetorDialogos;
    private dialogoNpc npcAtual;

    private int contadorDialogos = 0;
    private bool tocandoNpc = false;
    private objetoDialogo[] vetorcomdialogos;

    [Header("Configuração de Camada")]
    public LayerMask layerNpc;       // Selecione a layer "npc" no Inspetor
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        

        vetorDialogos = vetorcomdialogos;
        // Garante que a caixa de diálogo começa oculta ao iniciar o jogo
        if (painelDialogo != null)
        {
            painelDialogo.SetActive(false);
            textoCaixa.gameObject.SetActive(false);
            textoLocutor.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if ( (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ) && tocandoNpc )
            {
                if (contadorDialogos == vetorDialogos.Length) contadorDialogos = 0;
                textoCaixa.text = vetorDialogos[contadorDialogos].texto;
                textoLocutor.text = vetorDialogos[contadorDialogos].locutor;

                contadorDialogos++;
            }
    }

    // Deteta quando o player entra na área de colisão (Trigger) do NPC
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se o objeto tocado pertence à Layer "npc"
        if (((1 << collision.gameObject.layer) & layerNpc) != 0)
        {
            npcAtual = collision.GetComponent<dialogoNpc>();

            if (npcAtual != null)
                {
                    // Exemplo de como usar a variável do NPC (supondo que ele tenha uma variável pública chamada "nomeDoNpc")
                    vetorDialogos = npcAtual.vetorDialogosNpc;
                }

            if (painelDialogo != null)
                {
                    textoCaixa.gameObject.SetActive(true);
                    textoLocutor.gameObject.SetActive(true);
                    painelDialogo.SetActive(true);
                    tocandoNpc = true;
                     textoCaixa.text = vetorDialogos[0].texto;
                     textoLocutor.text = vetorDialogos[0].locutor;
                     contadorDialogos = 1;
                }

            
        }
    }

    // Deteta quando o player se afasta do NPC
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & layerNpc) != 0)
        {
            if (painelDialogo != null)
            {
                textoCaixa.gameObject.SetActive(false);
                textoLocutor.gameObject.SetActive(false);
                painelDialogo.SetActive(false);
                tocandoNpc = false;

                npcAtual = null;
            }
        }
    }
}

