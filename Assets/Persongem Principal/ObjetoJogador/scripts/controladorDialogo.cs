using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class controladorDialogo : MonoBehaviour
{

    [Header("Interface")]
    public GameObject painelDialogo; // Arraste o GameObject da sua caixa de diálogo aqui no Inspetor
    public TextMeshProUGUI textoCaixa;

    [Header("Configuração de Camada")]
    public LayerMask layerNpc;       // Selecione a layer "npc" no Inspetor
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textoCaixa.text = "A fimose dragons nunca me decepcionou mano, ela é muito legal, deveriamos eleger ela esse ano";
        // Garante que a caixa de diálogo começa oculta ao iniciar o jogo
        if (painelDialogo != null)
        {
            painelDialogo.SetActive(false);
            textoCaixa.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Deteta quando o player entra na área de colisão (Trigger) do NPC
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se o objeto tocado pertence à Layer "npc"
        if (((1 << collision.gameObject.layer) & layerNpc) != 0)
        {
            if (painelDialogo != null)
            {
                textoCaixa.gameObject.SetActive(true);
                painelDialogo.SetActive(true);
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
                painelDialogo.SetActive(false);
            }
        }
    }
}

