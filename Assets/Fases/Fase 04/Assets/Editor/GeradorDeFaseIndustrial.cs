// COLOQUE ESTE ARQUIVO EM: Assets/Editor/GeradorDeFaseIndustrial.cs
// Uso: menu Tools > Gerar Fase Industrial
#pragma warning disable CS0618
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GeradorDeFaseIndustrial : EditorWindow
{
    const int ColunasPorSecao = 40;
    const int Linhas = 14;
    const string CaracteresValidos = ".#=SCN12345XEBV";
    const string ChavesEntidades = "SCN12345XEBV";

    static readonly string[] NomesEntidades =
    {
        "Spawn_Jogador", "Cristal_Checkpoint", "NPC",
        "Inimigo_Patrulheiro", "Inimigo_Drone", "Inimigo_Saltador", "Inimigo_Torreta", "Perigo_SerraTrilho",
        "Porta_Saida", "Entrada_Boss", "Boss_Colosso", "Vitoria_Saida"
    };

    static readonly Color[] CoresEntidades =
    {
        new Color(0.2f, 0.9f, 0.3f), new Color(0.3f, 0.9f, 1f), new Color(1f, 1f, 0.4f),
        new Color(0.9f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.8f), new Color(1f, 0.55f, 0.1f),
        new Color(0.6f, 0.6f, 0.65f), new Color(1f, 1f, 1f),
        new Color(1f, 0.9f, 0.3f), new Color(0.5f, 0.3f, 0.8f), new Color(0.5f, 0.05f, 0.05f), new Color(0.3f, 1f, 0.5f)
    };

    // tamanhos em tiles (1 tile = 1 unidade com Pixels Per Unit = 32)
    static readonly Vector2[] TamanhosEntidades =
    {
        new Vector2(0.8f, 1.6f), new Vector2(0.6f, 1f), new Vector2(0.8f, 1.6f),
        new Vector2(1f, 0.9f), new Vector2(1f, 0.8f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
        new Vector2(1f, 2f), new Vector2(1f, 2f), new Vector2(3f, 3f), new Vector2(1f, 2f)
    };

    [SerializeField] TextAsset mapa;
    [SerializeField] DefaultAsset pastaTiles;
    [SerializeField] GameObject[] prefabs = new GameObject[12];

    readonly Dictionary<int, Tile> cacheTiles = new Dictionary<int, Tile>();
    readonly List<int> tilesFaltando = new List<int>();
    string pastaDosTiles;

    class Secao
    {
        public string titulo;
        public bool boss;
        public string[] linhas = new string[Linhas];
    }

    [MenuItem("Tools/Gerar Fase Industrial")]
    static void Abrir()
    {
        GetWindow<GeradorDeFaseIndustrial>("Fase Industrial");
    }

    void OnGUI()
    {
        if (prefabs == null || prefabs.Length != 12) prefabs = new GameObject[12];

        EditorGUILayout.HelpBox(
            "1) Arraste o arquivo mapa_industrial.txt.\n" +
            "2) Arraste a PASTA '1 Tiles' (as 81 imagens já com Pixels Per Unit = 32).\n" +
            "3) Clique em Gerar fase.", MessageType.Info);

        mapa = (TextAsset)EditorGUILayout.ObjectField("Mapa (txt)", mapa, typeof(TextAsset), false);
        pastaTiles = (DefaultAsset)EditorGUILayout.ObjectField("Pasta dos tiles", pastaTiles, typeof(DefaultAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs (opcional; vazio = caixa colorida)", EditorStyles.boldLabel);
        for (int i = 0; i < prefabs.Length; i++)
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(NomesEntidades[i], prefabs[i], typeof(GameObject), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Gerar fase", GUILayout.Height(32))) Gerar();
    }

    // ------------------------------------------------------------------

    void Gerar()
    {
        if (mapa == null || pastaTiles == null)
        {
            EditorUtility.DisplayDialog("Faltam arquivos", "Arraste o mapa (txt) e a pasta dos tiles.", "OK");
            return;
        }

        pastaDosTiles = AssetDatabase.GetAssetPath(pastaTiles);
        cacheTiles.Clear();
        tilesFaltando.Clear();

        var secoes = LerMapa(mapa.text);
        var principais = secoes.Where(s => !s.boss).ToList();
        var boss = secoes.FirstOrDefault(s => s.boss);
        if (principais.Count == 0)
        {
            EditorUtility.DisplayDialog("Mapa inválido", "Não encontrei seções no arquivo. Use o mapa_industrial.txt original.", "OK");
            return;
        }

        var antigo = GameObject.Find("FaseIndustrial");
        if (antigo != null) Undo.DestroyObjectImmediate(antigo);

        var raiz = new GameObject("FaseIndustrial");
        raiz.AddComponent<Grid>();
        Undo.RegisterCreatedObjectUndo(raiz, "Gerar fase industrial");

        var chao = CriarTilemap(raiz, "Chao", 0);
        var plataformas = CriarTilemap(raiz, "Plataformas", 1);

        var entidades = new GameObject("Entidades");
        entidades.transform.SetParent(raiz.transform, false);
        var contadores = new Dictionary<char, int>();

        // fase principal: todas as seções emendadas numa grade larga
        int largura = principais.Count * ColunasPorSecao;
        var grade = new char[largura, Linhas];
        for (int s = 0; s < principais.Count; s++)
            Copiar(principais[s], grade, s * ColunasPorSecao);
        Pintar(grade, largura, 0, chao, plataformas, entidades, contadores);

        // boss: seção isolada, bem longe da fase principal
        int deslocBoss = largura + 40;
        if (boss != null)
        {
            var gradeBoss = new char[ColunasPorSecao, Linhas];
            Copiar(boss, gradeBoss, 0);
            Pintar(gradeBoss, ColunasPorSecao, deslocBoss, chao, plataformas, entidades, contadores);
        }

        ConfigurarColisores(chao, plataformas);
        CriarZonaDeQueda(raiz, largura);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = raiz;

        string aviso = "Fase gerada: " + principais.Count + " trechos (" + largura + " tiles)";
        if (boss != null) aviso += " + arena do boss em x=" + deslocBoss + ".";
        if (tilesFaltando.Count > 0)
            aviso += "\n\nATENÇÃO: não achei os tiles: " + string.Join(", ", tilesFaltando.Distinct().OrderBy(n => n).Select(n => n.ToString("00"))) +
                     ". Confira se arrastou a pasta certa ('1 Tiles').";
        EditorUtility.DisplayDialog("Pronto!", aviso, "OK");
    }

    // ---------- leitura ----------

    List<Secao> LerMapa(string texto)
    {
        var lista = new List<Secao>();
        Secao atual = null;

        foreach (var bruta in texto.Split('\n'))
        {
            string l = bruta.TrimEnd('\r');

            if (l.StartsWith("---"))
            {
                atual = new Secao { titulo = l };
                atual.boss = l.ToUpperInvariant().Contains("BOSS");
                lista.Add(atual);
                continue;
            }

            if (atual == null || l.Length < 4 + ColunasPorSecao) continue;
            if (!int.TryParse(l.Substring(0, 2).Trim(), out int linha)) continue;
            if (linha < 0 || linha >= Linhas) continue;
            if (l.Substring(2, 2) != "  ") continue;

            string conteudo = l.Substring(4, ColunasPorSecao);
            if (conteudo.Any(ch => CaracteresValidos.IndexOf(ch) < 0)) continue;
            atual.linhas[linha] = conteudo;
        }
        return lista;
    }

    void Copiar(Secao s, char[,] destino, int deslocX)
    {
        for (int y = 0; y < Linhas; y++)
        {
            string linha = s.linhas[y] ?? new string('.', ColunasPorSecao);
            for (int x = 0; x < ColunasPorSecao; x++)
                destino[deslocX + x, y] = linha[x];
        }
    }

    // ---------- pintura ----------

    void Pintar(char[,] m, int w, int deslocX, Tilemap chao, Tilemap plataformas,
                GameObject entidades, Dictionary<char, int> contadores)
    {
        for (int y = 0; y < Linhas; y++)
        {
            for (int x = 0; x < w; x++)
            {
                char c = m[x, y];
                var pos = new Vector3Int(deslocX + x, Linhas - 1 - y, 0);

                if (c == '#') chao.SetTile(pos, TileChao(m, w, x, y));
                else if (c == '=') plataformas.SetTile(pos, TilePlataforma(m, w, x, y));
                else if (ChavesEntidades.IndexOf(c) >= 0)
                    CriarEntidade(deslocX + x, y, c, entidades, contadores);
            }
        }
    }

    static bool Solido(char[,] m, int w, int x, int y)
    {
        if (x < 0 || x >= w || y >= Linhas) return true;
        if (y < 0) return false;
        return m[x, y] == '#';
    }

    // Números dos arquivos IndustrialTile_NN.png
    Tile TileChao(char[,] m, int w, int x, int y)
    {
        bool aCima = !Solido(m, w, x, y - 1);
        bool aBaixo = !Solido(m, w, x, y + 1);
        bool aEsq = !Solido(m, w, x - 1, y);
        bool aDir = !Solido(m, w, x + 1, y);

        bool unico = aEsq && aDir;
        int pos = unico ? 1 : (aEsq ? 0 : (aDir ? 2 : 1)); // 0 = esquerda, 1 = meio, 2 = direita

        int n;
        if (aCima && aBaixo) n = unico ? 25 : (pos == 0 ? 55 : (pos == 2 ? 57 : 56)); // placa de 1 tile
        else if (aCima) n = unico ? 25 : (pos == 0 ? 4 : (pos == 2 ? 6 : 5));          // topo com brilho
        else if (aBaixo) n = unico ? 66 : (pos == 0 ? 22 : (pos == 2 ? 24 : 23));      // base
        else n = unico ? 65 : (pos == 0 ? 13 : (pos == 2 ? 15 : 14));                  // miolo
        return PegarTile(n);
    }

    Tile TilePlataforma(char[,] m, int w, int x, int y)
    {
        bool temE = x > 0 && m[x - 1, y] == '=';
        bool temD = x < w - 1 && m[x + 1, y] == '=';
        int n = (!temE && temD) ? 77 : (temE && !temD) ? 79 : 78; // esquerda, direita, meio (ou única)
        return PegarTile(n);
    }

    Tile PegarTile(int numero)
    {
        if (cacheTiles.TryGetValue(numero, out Tile existente)) return existente;

        string caminhoSprite = pastaDosTiles + "/IndustrialTile_" + numero.ToString("00") + ".png";
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(caminhoSprite);
        if (sp == null)
        {
            tilesFaltando.Add(numero);
            cacheTiles[numero] = null;
            return null;
        }

        const string pasta = "Assets/TilesIndustrial";
        if (!AssetDatabase.IsValidFolder(pasta)) AssetDatabase.CreateFolder("Assets", "TilesIndustrial");

        string caminho = pasta + "/Tile_" + numero.ToString("00") + ".asset";
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

        cacheTiles[numero] = t;
        return t;
    }

    // ---------- objetos da cena ----------

    Tilemap CriarTilemap(GameObject pai, string nome, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = ordem;
        return tm;
    }

    void ConfigurarColisores(Tilemap chao, Tilemap plataformas)
    {
        var tcChao = chao.gameObject.AddComponent<TilemapCollider2D>();
        UsarComposto(tcChao);
        chao.gameObject.AddComponent<CompositeCollider2D>();
        chao.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

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
    }

    static void UsarComposto(TilemapCollider2D tc)
    {
#if UNITY_2023_1_OR_NEWER
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        tc.usedByComposite = true;
#endif
    }

    // Gatilho invisível abaixo da fase: cair nos vãos = perder vida / voltar ao checkpoint
    void CriarZonaDeQueda(GameObject raiz, int largura)
    {
        var go = new GameObject("ZonaDeQueda");
        go.transform.SetParent(raiz.transform, false);
        go.transform.position = new Vector3(largura * 0.5f, -4f, 0f);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(largura + 40f, 2f);
    }

    void CriarEntidade(int gx, int y, char c, GameObject pai, Dictionary<char, int> contadores)
    {
        int idx = ChavesEntidades.IndexOf(c);
        contadores.TryGetValue(c, out int n);
        contadores[c] = ++n;

        string nome = NomesEntidades[idx] + "_" + n;
        Vector2 tam = TamanhosEntidades[idx];
        float baseY = Linhas - 1 - y;
        bool aerea = (c == '2' || c == '5'); // drone e serra ficam no ar: a linha marca o centro

        GameObject go;
        if (prefabs != null && idx < prefabs.Length && prefabs[idx] != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[idx]);
            go.transform.SetParent(pai.transform, false);
            go.transform.position = aerea
                ? new Vector3(gx + 0.5f, baseY + 0.5f, 0f)
                : new Vector3(gx + 0.5f, baseY, 0f); // pivô nos pés
        }
        else
        {
            go = new GameObject(nome);
            go.transform.SetParent(pai.transform, false);
            float cy = aerea ? baseY + 0.5f : baseY + tam.y * 0.5f;
            go.transform.position = new Vector3(gx + 0.5f, cy, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = tam;
            sr.color = CoresEntidades[idx];
            sr.sortingOrder = 10;
        }

        go.name = nome;
        Undo.RegisterCreatedObjectUndo(go, "Gerar fase industrial");
    }
}
