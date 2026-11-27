using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelecaoArmas : MonoBehaviour
{
    [Header("Cards na ordem: espada, lanca, machado, revolver")]
    [SerializeField] private Image[] cards = new Image[4];
    [SerializeField] private Sprite[] spritesNormais = new Sprite[4];
    [SerializeField] private Sprite[] spritesSelecionados = new Sprite[4];
    [SerializeField] private Sprite[] retratosArmas = new Sprite[4];
    [SerializeField] private int[] idsArmasPorCard = { 1, 2, 3, 4 };
    [SerializeField] private Image retratoPreview;
    [SerializeField] private string cenaSemCheckpoint = "Tutorial";

    private readonly List<int> armasEscolhidas = new List<int>(2);
    private int armaEmPreview = -1;

    private void Start()
    {
        if (retratoPreview != null) retratoPreview.enabled = false;

        for (int indice = 0; indice < cards.Length; indice++)
        {
            int indiceCapturado = indice;
            if (cards[indice] == null) continue;

            Button botao = cards[indice].GetComponent<Button>();
            if (botao == null) botao = cards[indice].gameObject.AddComponent<Button>();
            botao.targetGraphic = cards[indice];
            botao.onClick.AddListener(() => AlternarSelecao(indiceCapturado));

            EventTrigger eventos = cards[indice].GetComponent<EventTrigger>();
            if (eventos == null) eventos = cards[indice].gameObject.AddComponent<EventTrigger>();
            if (eventos.triggers == null) eventos.triggers = new List<EventTrigger.Entry>();

            AdicionarEventoPonteiro(eventos, EventTriggerType.PointerEnter, () => MostrarPreview(indiceCapturado));
            AdicionarEventoPonteiro(eventos, EventTriggerType.PointerExit, () => EsconderPreview(indiceCapturado));
            AtualizarVisual(indice);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ConfirmarSelecao();
        }
    }

    private void AlternarSelecao(int indice)
    {
        if (indice < 0 || indice >= idsArmasPorCard.Length) return;

        int idArma = idsArmasPorCard[indice];
        if (armasEscolhidas.Contains(idArma))
        {
            armasEscolhidas.Remove(idArma);
        }
        else if (armasEscolhidas.Count < 2)
        {
            armasEscolhidas.Add(idArma);
        }

        AtualizarVisual(indice);
    }

    private void AtualizarVisual(int indice)
    {
        if (indice < 0 || indice >= cards.Length || cards[indice] == null) return;
        bool selecionada = armasEscolhidas.Contains(idsArmasPorCard[indice]);
        if (indice < spritesNormais.Length && indice < spritesSelecionados.Length)
        {
            Sprite sprite = selecionada ? spritesSelecionados[indice] : spritesNormais[indice];
            if (sprite != null) cards[indice].sprite = sprite;
        }
    }

    private void MostrarPreview(int indice)
    {
        armaEmPreview = indice;
        if (retratoPreview == null || indice < 0 || indice >= retratosArmas.Length || retratosArmas[indice] == null) return;

        retratoPreview.sprite = retratosArmas[indice];
        retratoPreview.enabled = true;
    }

    private void EsconderPreview(int indice)
    {
        if (armaEmPreview != indice) return;
        armaEmPreview = -1;
        if (retratoPreview != null) retratoPreview.enabled = false;
    }

    private void ConfirmarSelecao()
    {
        if (armasEscolhidas.Count != 2)
        {
            Debug.Log("Escolha exatamente duas armas antes de continuar.", this);
            return;
        }

        PlayerPrefs.SetInt("ArmasSelecionadas", 2);
        PlayerPrefs.SetInt("ArmaEsquerdaId", armasEscolhidas[0]);
        PlayerPrefs.SetInt("ArmaDireitaId", armasEscolhidas[1]);
        PlayerPrefs.SetInt("ItemCimaCuraDisponivel", 1);
        PlayerPrefs.SetInt("ItemBaixoCuraDisponivel", 1);
        PlayerPrefs.SetInt("SlotRodaAtual", 3);
        PlayerPrefs.Save();

        GerenciadorCheckpoint.CarregarCheckpointOuCenaInicial(cenaSemCheckpoint);
    }

    private static void AdicionarEventoPonteiro(EventTrigger eventos, EventTriggerType tipo, Action callback)
    {
        EventTrigger.Entry entrada = new EventTrigger.Entry { eventID = tipo };
        entrada.callback.AddListener((_) => callback?.Invoke());
        eventos.triggers.Add(entrada);
    }
}