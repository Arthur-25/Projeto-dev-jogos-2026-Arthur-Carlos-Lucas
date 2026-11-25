using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControladorSelecaoArmas : MonoBehaviour
{
    [System.Serializable]
    public class CardArmaUI
    {
        public string nomeCard;
        public int idArma = 1; // 1=Espada, 2=Lanca, 3=Machado, 4=Revolver
        public Image imagemCard;
        public Sprite spriteContornoAzul;
        public Sprite spriteContornoAmarelo;
        public Sprite spriteBonecoComArma;
    }

    [Header("Cards e Prévia")]
    public CardArmaUI[] cards = new CardArmaUI[4];
    public Image imagemBonecoMosher; // Arraste o objeto 'mosher' aqui
    public Sprite spriteBonecoSemArma;

    [Header("Fluxo de Cena")]
    public string cenaFallbackSemCheckpoint = "Tutorial";
    public int maximoArmasSelecionadas = 2;

    // Guarda a ordem exata da escolha: [0] -> Esquerda, [1] -> Direita
    private readonly List<int> armasSelecionadas = new List<int>();

    void Start()
    {
        if (imagemBonecoMosher == null)
        {
            GameObject mosherObj = GameObject.Find("mosher");
            if (mosherObj != null) imagemBonecoMosher = mosherObj.GetComponent<Image>();
        }

        for (int i = 0; i < cards.Length; i++)
        {
            CardArmaUI card = cards[i];
            if (card == null || card.imagemCard == null) continue;

            Button btn = card.imagemCard.GetComponent<Button>();
            if (btn == null) btn = card.imagemCard.gameObject.AddComponent<Button>();

            int indiceCapturado = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => AlternarSelecaoCard(indiceCapturado));

            EventTrigger trigger = card.imagemCard.GetComponent<EventTrigger>();
            if (trigger == null) trigger = card.imagemCard.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entryEnter.callback.AddListener((_) => MostrarPreviaBoneco(indiceCapturado));
            trigger.triggers.Add(entryEnter);
        }

        AtualizarVisualCards();
        if (imagemBonecoMosher != null && spriteBonecoSemArma != null)
        {
            imagemBonecoMosher.sprite = spriteBonecoSemArma;
        }
    }

    void Update()
    {
        bool apertouEnter = (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (apertouEnter)
        {
            ConfirmarESeguir();
        }
    }

    public void AlternarSelecaoCard(int indiceCard)
    {
        if (indiceCard < 0 || indiceCard >= cards.Length || cards[indiceCard] == null) return;

        int idArma = cards[indiceCard].idArma;

        if (armasSelecionadas.Contains(idArma))
        {
            armasSelecionadas.Remove(idArma);
        }
        else
        {
            if (armasSelecionadas.Count >= maximoArmasSelecionadas)
            {
                return;
            }
            armasSelecionadas.Add(idArma);
        }

        MostrarPreviaBoneco(indiceCard);
        AtualizarVisualCards();
    }

    public void MostrarPreviaBoneco(int indiceCard)
    {
        if (imagemBonecoMosher == null || indiceCard < 0 || indiceCard >= cards.Length) return;
        CardArmaUI card = cards[indiceCard];
        if (card != null && card.spriteBonecoComArma != null)
        {
            imagemBonecoMosher.sprite = card.spriteBonecoComArma;
        }
    }

    private void AtualizarVisualCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            CardArmaUI card = cards[i];
            if (card == null || card.imagemCard == null) continue;

            bool estaSelecionada = armasSelecionadas.Contains(card.idArma);
            Sprite alvo = estaSelecionada ? card.spriteContornoAmarelo : card.spriteContornoAzul;
            if (alvo != null)
            {
                card.imagemCard.sprite = alvo;
            }
        }
    }

    public void ConfirmarESeguir()
    {
        int idEsquerda = armasSelecionadas.Count > 0 ? armasSelecionadas[0] : 0;
        int idDireita = armasSelecionadas.Count > 1 ? armasSelecionadas[1] : 0;

        PlayerPrefs.SetInt("ArmaEsquerdaId", idEsquerda);
        PlayerPrefs.SetInt("ArmaDireitaId", idDireita);
        PlayerPrefs.SetInt("ItemCimaCuraDisponivel", 1);
        PlayerPrefs.SetInt("ItemBaixoCuraDisponivel", 1);
        PlayerPrefs.SetInt("SlotRodaAtual", idEsquerda > 0 ? 3 : (idDireita > 0 ? 1 : 0));
        PlayerPrefs.Save();

        GerenciadorCheckpoint.CarregarCheckpointOuCenaInicial(cenaFallbackSemCheckpoint);
    }
}