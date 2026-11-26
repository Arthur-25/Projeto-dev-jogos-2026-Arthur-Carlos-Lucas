// COLOQUE ESTE ARQUIVO EM: Assets/Editor/GeradorDeFase.cs
// Uso: menu Tools > Gerar Fase
#pragma warning disable CS0618
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GeradorDeFase : EditorWindow
{
    const int ColunasPorSecao = 40;
    const int Linhas = 14;
    const string CaracteresValidos = ".#=W~SXDCN12345";
    const string ChavesEntidades = "SXDCN12345";

    static readonly string[] NomesEntidades =
    {
        "Porta_Inicio", "Porta_Saida", "Porta_Secreta", "Cristal_Checkpoint", "NPC",
        "Inimigo_Patrulheiro", "Inimigo_Voador", "Inimigo_Sapo", "Inimigo_Planta", "Boss_Guardiao"
    };

    static readonly Color[] CoresEntidades =
    {
        new Color(0.2f, 0.8f, 0.3f), new Color(1f, 0.9f, 0.3f), new Color(0.5f, 0.3f, 0.8f),
        new Color(0.3f, 0.9f, 1f), new Color(1f, 1f, 0.4f),
        new Color(0.9f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.8f), new Color(1f, 0.55f, 0.1f),
        new Color(0.1f, 0.5f, 0.1f), new Color(0.5f, 0.05f, 0.05f)
    };

    static readonly Vector2[] TamanhosEntidades =
    {
        new Vector2(1f, 2f), new Vector2(1f, 2f), new Vector2(1f, 2f),
        new Vector2(0.6f, 0.9f), new Vector2(0.8f, 1f),
        new Vector2(0.9f, 0.7f), new Vector2(0.9f, 0.6f), new Vector2(0.9f, 0.8f),
        new Vector2(0.8f, 1f), new Vector2(2f, 2f)
    };

    [SerializeField] TextAsset mapa;
    [SerializeField] Texture2D tileset;
    [SerializeField] int tamanhoTile = 16;
    [SerializeField] GameObject[] prefabs = new GameObject[10];

    Sprite[] sprites;
    readonly Dictionary<string, Tile> cacheTiles = new Dictionary<string, Tile>();
    char[,] celulas;
    int largura;

    [MenuItem("Tools/Gerar Fase")]
    static void Abrir()
    {
        GetWindow<GeradorDeFase>("Gerar Fase");
    }

    void OnGUI()
    {
        if (prefabs == null || prefabs.Length != 10) prefabs = new GameObject[10];

        EditorGUILayout.HelpBox(
            "1) Arraste o tileset (já fatiado em 16x16) e o arquivo mapa_fase.txt.\n2) Clique em Gerar fase.",
            MessageType.Info);

        mapa = (TextAsset)EditorGUILayout.ObjectField("Mapa (txt)", mapa, typeof(TextAsset), false);
        tileset = (Texture2D)EditorGUILayout.ObjectField("Tileset do terreno", tileset, typeof(Texture2D), false);
        tamanhoTile = EditorGUILayout.IntField("Tamanho do tile (px)", tamanhoTile);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs (opcional; vazio = caixa colorida)", EditorStyles.boldLabel);
        for (int i = 0; i < prefabs.Length; i++)
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(NomesEntidades[i], prefabs[i], typeof(GameObject), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Gerar fase", GUILayout.Height(32)))
            Gerar();
    }

    void Gerar()
    {
        if (mapa == null || tileset == null)
        {
            EditorUtility.DisplayDialog("Faltam arquivos", "Arraste o mapa (txt) e o tileset para os campos.", "OK");
            return;
        }

        string caminho = AssetDatabase.GetAssetPath(tileset);
        sprites = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().ToArray();
        if (sprites.Length < 2)
        {
            EditorUtility.DisplayDialog("Tileset sem fatias",
                "A imagem ainda não foi fatiada. Selecione o PNG, mude Sprite Mode para Multiple, abra o Sprite Editor e use Slice > Grid By Cell Size 16x16.",
                "OK");
            return;
        }

        cacheTiles.Clear();
        if (!LerMapa(mapa.text)) return;

        var antigo = GameObject.Find("Fase");
        if (antigo != null) Undo.DestroyObjectImmediate(antigo);

        var raiz = new GameObject("Fase");
        raiz.AddComponent<Grid>();
        Undo.RegisterCreatedObjectUndo(raiz, "Gerar fase");

        var chao = CriarTilemap(raiz, "Chao", 0);
        var plataformas = CriarTilemap(raiz, "Plataformas", 1);
        var agua = CriarTilemap(raiz, "Agua", 2);

        var entidades = new GameObject("Entidades");
        entidades.transform.SetParent(raiz.transform, false);
        var contadores = new Dictionary<char, int>();

        for (int y = 0; y < Linhas; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                char c = celulas[x, y];
                var pos = new Vector3Int(x, Linhas - 1 - y, 0);

                switch (c)
                {
                    case '#': chao.SetTile(pos, TileChao(x, y)); break;
                    case '=': plataformas.SetTile(pos, TilePlataforma(x, y)); break;
                    case 'W': plataformas.SetTile(pos, TilePonte(x, y)); break;
                    case '~': agua.SetTile(pos, PegarTile(7, 6)); break;
                    case '.': break;
                    default:
                        if (ChavesEntidades.IndexOf(c) >= 0)
                            CriarEntidade(x, y, c, entidades, contadores);
                        break;
                }
            }
        }

        ConfigurarColisores(chao, plataformas, agua);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = raiz;
        EditorUtility.DisplayDialog("Pronto!",
            "Fase gerada com " + (largura / ColunasPorSecao) + " trechos (" + largura + " tiles de largura).\nProcure o objeto 'Fase' na Hierarchy.",
            "OK");
    }

    // ---------- Leitura do mapa ----------

    bool LerMapa(string texto)
    {
        var secoes = new List<List<string>>();
        List<string> atual = null;

        foreach (var bruta in texto.Split('\n'))
        {
            string l = bruta.TrimEnd('\r');

            if (l.StartsWith("---"))
            {
                atual = new List<string>();
                secoes.Add(atual);
                continue;
            }

            if (atual == null || l.Length < 4 + ColunasPorSecao) continue;
            if (!int.TryParse(l.Substring(0, 2).Trim(), out int linha)) continue;
            if (linha < 0 || linha >= Linhas) continue;
            if (l.Substring(2, 2) != "  ") continue;

            string conteudo = l.Substring(4, ColunasPorSecao);
            if (conteudo.Any(ch => CaracteresValidos.IndexOf(ch) < 0)) continue;

            while (atual.Count <= linha) atual.Add(null);
            atual[linha] = conteudo;
        }

        if (secoes.Count == 0)
        {
            EditorUtility.DisplayDialog("Mapa inválido",
                "Não encontrei nenhum trecho no arquivo. Use o mapa_fase.txt original.", "OK");
            return false;
        }

        largura = secoes.Count * ColunasPorSecao;
        celulas = new char[largura, Linhas];

        for (int s = 0; s < secoes.Count; s++)
        {
            for (int y = 0; y < Linhas; y++)
            {
                string linha = (y < secoes[s].Count && secoes[s][y] != null)
                    ? secoes[s][y]
                    : new string('.', ColunasPorSecao);

                for (int cx = 0; cx < ColunasPorSecao; cx++)
                    celulas[s * ColunasPorSecao + cx, y] = linha[cx];
            }
        }
        return true;
    }

    bool Solido(int x, int y)
    {
        if (x < 0 || x >= largura || y >= Linhas) return true; // bordas do mapa contam como chão
        if (y < 0) return false;
        return celulas[x, y] == '#';
    }

    bool E(int x, int y, char c)
    {
        return x >= 0 && x < largura && y >= 0 && y < Linhas && celulas[x, y] == c;
    }

    // ---------- Escolha dos tiles (coluna, linha) no tileset ----------
    // Se algum tile aparecer errado, ajuste os números aqui.

    Tile TileChao(int x, int y)
    {
        bool aCima = !Solido(x, y - 1);
        bool aBaixo = !Solido(x, y + 1);
        bool aEsq = !Solido(x - 1, y);
        bool aDir = !Solido(x + 1, y);

        int col;
        if (aEsq && !aDir) col = 0;           // borda esquerda
        else if (aDir && !aEsq) col = 3;      // borda direita
        else col = 1 + (x % 2);               // miolo (alterna 1 e 2)

        int lin;
        if (aCima && aBaixo) lin = 3;         // fatia fina (1 tile de altura)
        else if (aCima) lin = 0;              // topo com grama
        else if (aBaixo) lin = 2;             // base
        else lin = 1;                         // miolo

        return PegarTile(col, lin);
    }

    Tile TilePlataforma(int x, int y)
    {
        bool temE = E(x - 1, y, '=');
        bool temD = E(x + 1, y, '=');
        int col;
        if (!temE && temD) col = 0;
        else if (temE && !temD) col = 3;
        else if (temE && temD) col = 1 + (x % 2);
        else col = 1;
        return PegarTile(col, 3);
    }

    Tile TilePonte(int x, int y)
    {
        bool temE = E(x - 1, y, 'W');
        bool temD = E(x + 1, y, 'W');
        int col = (!temE && temD) ? 4 : (temE && !temD) ? 6 : 5;
        return PegarTile(col, 6);
    }

    // ---------- Sprites e tiles ----------

    Sprite AcharSprite(int col, int lin)
    {
        foreach (var s in sprites)
        {
            int c = Mathf.RoundToInt(s.rect.x / tamanhoTile);
            int l = Mathf.RoundToInt((tileset.height - s.rect.yMax) / tamanhoTile);
            if (c == col && l == lin) return s;
        }
        return null;
    }

    Tile PegarTile(int col, int lin)
    {
        string chave = col + "_" + lin;
        if (cacheTiles.TryGetValue(chave, out Tile existente)) return existente;

        Sprite sp = AcharSprite(col, lin);
        if (sp == null) { cacheTiles[chave] = null; return null; }

        const string pasta = "Assets/TilesGerados";
        if (!AssetDatabase.IsValidFolder(pasta)) AssetDatabase.CreateFolder("Assets", "TilesGerados");

        string caminho = pasta + "/Tile_" + col + "_" + lin + ".asset";
        var t = AssetDatabase.LoadAssetAtPath<Tile>(caminho);
        if (t == null)
        {
            t = CreateInstance<Tile>();
            t.sprite = sp;
            AssetDatabase.CreateAsset(t, caminho);
        }
        else
        {
            t.sprite = sp;
            EditorUtility.SetDirty(t);
        }

        cacheTiles[chave] = t;
        return t;
    }

    // ---------- Objetos da cena ----------

    Tilemap CriarTilemap(GameObject pai, string nome, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = ordem;
        return tm;
    }

    void ConfigurarColisores(Tilemap chao, Tilemap plataformas, Tilemap agua)
    {
        // Chão: colisão sólida unida num único colisor
        var tcChao = chao.gameObject.AddComponent<TilemapCollider2D>();
        UsarComposto(tcChao);
        chao.gameObject.AddComponent<CompositeCollider2D>();
        chao.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        // Plataformas e ponte: só se pisa por cima (one-way)
        var tcPlat = plataformas.gameObject.AddComponent<TilemapCollider2D>();
        UsarComposto(tcPlat);
        var ccPlat = plataformas.gameObject.AddComponent<CompositeCollider2D>();
        ccPlat.geometryType = CompositeCollider2D.GeometryType.Polygons;
        ccPlat.usedByEffector = true;
        plataformas.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        var ef = plataformas.gameObject.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true;
        ef.useSideFriction = false;
        ef.surfaceArc = 170f;

        // Água: gatilho de dano (trate no seu script de jogador via OnTriggerEnter2D)
        var tcAgua = agua.gameObject.AddComponent<TilemapCollider2D>();
        tcAgua.isTrigger = true;
    }

    static void UsarComposto(TilemapCollider2D tc)
    {
#if UNITY_2023_1_OR_NEWER
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        tc.usedByComposite = true;
#endif
    }

    void CriarEntidade(int x, int y, char c, GameObject pai, Dictionary<char, int> contadores)
    {
        int idx = ChavesEntidades.IndexOf(c);
        contadores.TryGetValue(c, out int n);
        contadores[c] = ++n;

        string nome = NomesEntidades[idx] + "_" + n;
        Vector2 tam = TamanhosEntidades[idx];
        float baseY = Linhas - 1 - y; // base da célula no mundo
        GameObject go;

        if (prefabs != null && idx < prefabs.Length && prefabs[idx] != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[idx]);
            go.transform.SetParent(pai.transform, false);
            go.transform.position = new Vector3(x + 0.5f, baseY, 0f); // pivô nos pés
        }
        else
        {
            go = new GameObject(nome);
            go.transform.SetParent(pai.transform, false);
            go.transform.position = new Vector3(x + 0.5f, baseY + tam.y * 0.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = tam;
            sr.color = CoresEntidades[idx];
            sr.sortingOrder = 10;
        }

        go.name = nome;
        Undo.RegisterCreatedObjectUndo(go, "Gerar fase");
    }
}
