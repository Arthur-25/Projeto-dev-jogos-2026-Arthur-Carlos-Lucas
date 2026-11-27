using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class controladorDialogo : MonoBehaviour
{
    [Header("Interface")]
    public GameObject painelDialogo;
    public TextMeshProUGUI textoCaixa;
    public TextMeshProUGUI textoLocutor;
    public Image imagemIcone;
    public objetoDialogo[] vetorDialogos;

    private dialogoNpc npcAtual;
    private int contadorDialogos = 0;
    private bool tocandoNpc = false;

    [Header("Configuração de Camada")]
    public LayerMask layerNpc;

    void Start()
    {
        if (painelDialogo != null)
        {
            painelDialogo.SetActive(false);
            if (textoCaixa != null) textoCaixa.gameObject.SetActive(false);
            if (textoLocutor != null) textoLocutor.gameObject.SetActive(false);
            if (imagemIcone != null) imagemIcone.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!tocandoNpc || vetorDialogos == null || vetorDialogos.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (contadorDialogos >= vetorDialogos.Length)
            {
                contadorDialogos = 0;
            }

            ExibirDialogoNoIndice(contadorDialogos);
            contadorDialogos++;
        }
    }

    private void ExibirDialogoNoIndice(int indice)
    {
        if (vetorDialogos == null || indice < 0 || indice >= vetorDialogos.Length) return;

        objetoDialogo dialogo = vetorDialogos[indice];
        if (dialogo == null) return;

        if (textoCaixa != null) textoCaixa.text = dialogo.texto;
        if (textoLocutor != null) textoLocutor.text = dialogo.locutor;

        if (imagemIcone != null)
        {
            if (dialogo.icone != null)
            {
                imagemIcone.gameObject.SetActive(true);
                imagemIcone.sprite = dialogo.icone;
            }
            else
            {
                imagemIcone.gameObject.SetActive(false);
            }
        }

        if (npcAtual != null)
        {
            npcAtual.IrParaDialogo(indice);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & layerNpc) != 0)
        {
            npcAtual = collision.GetComponent<dialogoNpc>();

            if (npcAtual != null)
            {
                vetorDialogos = npcAtual.vetorDialogosNpc;
            }

            if (painelDialogo != null && vetorDialogos != null && vetorDialogos.Length > 0)
            {
                if (textoCaixa != null) textoCaixa.gameObject.SetActive(true);
                if (textoLocutor != null) textoLocutor.gameObject.SetActive(true);
                painelDialogo.SetActive(true);
                tocandoNpc = true;

                if (npcAtual != null)
                {
                    npcAtual.IniciarDialogo(0);
                }

                ExibirDialogoNoIndice(0);
                contadorDialogos = 1;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & layerNpc) != 0)
        {
            if (npcAtual != null)
            {
                npcAtual.EncerrarDialogo();
            }

            if (painelDialogo != null)
            {
                if (textoCaixa != null) textoCaixa.gameObject.SetActive(false);
                if (textoLocutor != null) textoLocutor.gameObject.SetActive(false);
                if (imagemIcone != null) imagemIcone.gameObject.SetActive(false);
                painelDialogo.SetActive(false);
            }

            tocandoNpc = false;
            npcAtual = null;
        }
    }
}