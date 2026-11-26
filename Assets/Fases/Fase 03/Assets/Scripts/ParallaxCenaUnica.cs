// COLOQUE ESTE ARQUIVO EM: Assets/Scripts/ParallaxCenaUnica.cs  (NÃO dentro da pasta Editor)
// Use no objeto "Fundo" (filho da Main Camera) que contém as 7 camadas BackGround_001..007.
using System.Collections.Generic;
using UnityEngine;

public class ParallaxCenaUnica : MonoBehaviour
{
    [System.Serializable]
    public class Camada
    {
        public Transform objeto;

        [Tooltip("0 = não se mexe (céu). 1 = anda o máximo possível (primeiro plano).")]
        [Range(0f, 1f)] public float forca = 0.5f;
    }

    [Tooltip("Vazio = Main Camera.")]
    public Camera cam;

    public Camada[] camadas;

    [Header("Trajeto da câmera na fase (posição X do mundo)")]
    [Tooltip("X da câmera quando o jogador está no começo da fase.")]
    public float xInicio = 12f;
    [Tooltip("X da câmera quando o jogador chega ao fim da fase.")]
    public float xFim = 188f;

    [Header("Movimento vertical (subidas da fase)")]
    public bool usarVertical = true;
    public float yInicio = 4f;
    public float yFim = 9f;
    [Range(0f, 1f)] public float forcaVertical = 0.5f;

    bool avisou;

    void Reset()
    {
        PreencherAutomatico();
    }

    [ContextMenu("Preencher camadas automaticamente")]
    void PreencherAutomatico()
    {
        var lista = new List<Camada>();
        foreach (Transform filho in transform)
        {
            if (filho.GetComponent<SpriteRenderer>() == null) continue;
            lista.Add(new Camada { objeto = filho, forca = ForcaPeloNome(filho.name) });
        }
        camadas = lista.ToArray();
    }

    // BackGround_001 = mais perto (move mais) ... BackGround_007 = céu (parado)
    static float ForcaPeloNome(string nome)
    {
        int fim = nome.Length;
        int ini = fim;
        while (ini > 0 && char.IsDigit(nome[ini - 1])) ini--;

        if (ini < fim && int.TryParse(nome.Substring(ini), out int n))
        {
            switch (n)
            {
                case 1: return 1.00f;
                case 2: return 0.75f;
                case 3: return 0.55f;
                case 4: return 0.35f;
                case 5: return 0.20f;
                case 6: return 0.10f;
                case 7: return 0.00f;
            }
        }
        return 0.5f;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        if (camadas == null || camadas.Length == 0) PreencherAutomatico();

        float alturaVista = cam.orthographicSize * 2f;
        float larguraVista = alturaVista * cam.aspect;

        float px = Mathf.InverseLerp(xInicio, xFim, cam.transform.position.x);
        float py = Mathf.InverseLerp(yInicio, yFim, cam.transform.position.y);

        Vector3 escalaPai = transform.lossyScale;
        if (Mathf.Approximately(escalaPai.x, 0f) || Mathf.Approximately(escalaPai.y, 0f)) return;

        foreach (var c in camadas)
        {
            if (c == null || c.objeto == null) continue;
            var sr = c.objeto.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            Vector2 tam = sr.bounds.size; // tamanho da camada no mundo

            if (!avisou && (tam.x < larguraVista || tam.y < alturaVista))
            {
                avisou = true;
                Debug.LogWarning("ParallaxCenaUnica: a camada '" + c.objeto.name +
                    "' é menor que a tela e vai mostrar bordas vazias. Aumente a Scale do objeto 'Fundo'.");
            }

            // folga disponível em cada lado: é o quanto dá para deslizar sem aparecer borda vazia
            float margemX = Mathf.Max(0f, (tam.x - larguraVista) * 0.5f);
            float margemY = Mathf.Max(0f, (tam.y - alturaVista) * 0.5f);

            float dx = Mathf.Lerp(1f, -1f, px) * margemX * c.forca;
            float dy = usarVertical
                ? Mathf.Lerp(1f, -1f, py) * margemY * c.forca * forcaVertical
                : 0f;

            Vector3 lp = c.objeto.localPosition;
            lp.x = dx / escalaPai.x;
            lp.y = dy / escalaPai.y;
            c.objeto.localPosition = lp;
        }
    }
}
