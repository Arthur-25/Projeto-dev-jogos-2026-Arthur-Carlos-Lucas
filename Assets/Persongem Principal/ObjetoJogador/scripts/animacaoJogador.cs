using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class animacaoJogador : MonoBehaviour
{
    [System.Serializable]
    public struct ClipeSprite
    {
        public string nome;
        [Tooltip("Arraste o Sprite Sheet (Texture2D) fatiado aqui para pegar os quadros de uma vez.")]
        public Texture2D spriteSheet;
        [Tooltip("Indice inicial dentro dos sprites extraidos do sheet (0 = comeco).")]
        [Min(0)] public int frameInicial;
        [Tooltip("0 = pega todos a partir do frame inicial.")]
        [Min(0)] public int quantidadeFrames;
        [Tooltip("Opcional: preencha apenas se quiser sobrescrever manualmente em vez de usar o spriteSheet.")]
        public Sprite[] frames;
        [Min(0.1f)]
        public float fps;
        public bool loop;

        public Sprite[] ObterFramesResolvidos()
        {
            if (frames != null && frames.Length > 0)
            {
                return frames;
            }

            if (spriteSheet == null) return null;

#if UNITY_EDITOR
            string caminho = AssetDatabase.GetAssetPath(spriteSheet);
            if (!string.IsNullOrEmpty(caminho))
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(caminho);
                List<Sprite> extraidos = new List<Sprite>();

                foreach (Object obj in subAssets)
                {
                    if (obj is Sprite sp)
                    {
                        extraidos.Add(sp);
                    }
                }

                // Ordena pela posicao (cima para baixo, esquerda para direita) caso venham fora de ordem
                extraidos.Sort((a, b) =>
                {
                    int cmpY = b.rect.y.CompareTo(a.rect.y);
                    if (cmpY != 0) return cmpY;
                    return a.rect.x.CompareTo(b.rect.x);
                });

                if (extraidos.Count == 0) return null;

                int inicio = Mathf.Clamp(frameInicial, 0, extraidos.Count - 1);
                int total = quantidadeFrames > 0
                    ? Mathf.Min(quantidadeFrames, extraidos.Count - inicio)
                    : extraidos.Count - inicio;

                return extraidos.GetRange(inicio, total).ToArray();
            }
#endif
            return null;
        }
    }

    [Header("Animacoes Base")]
    [SerializeField] private ClipeSprite idle = new ClipeSprite { nome = "idle", fps = 8f, loop = true };
    [SerializeField] private ClipeSprite walk = new ClipeSprite { nome = "walk", fps = 12f, loop = true };
    [SerializeField] private ClipeSprite jump = new ClipeSprite { nome = "jump", fps = 10f, loop = false };
    [SerializeField] private ClipeSprite death = new ClipeSprite { nome = "death", fps = 10f, loop = false };

    [Header("Animacoes de Ataque")]
    [SerializeField] private ClipeSprite sword = new ClipeSprite { nome = "sword", fps = 16f, loop = false };
    [SerializeField] private ClipeSprite spear = new ClipeSprite { nome = "spear", fps = 16f, loop = false };
    [SerializeField] private ClipeSprite axe = new ClipeSprite { nome = "axe", fps = 14f, loop = false };
    [SerializeField] private ClipeSprite gun = new ClipeSprite { nome = "gun", fps = 16f, loop = false };
    [SerializeField] private ClipeSprite pogo = new ClipeSprite { nome = "pogo", fps = 16f, loop = false };

    private SpriteRenderer spriteRenderer;
    private ClipeSprite clipeAtual;
    private Sprite[] framesAtuais;
    private string nomeAtual;
    private int indiceFrame;
    private float timerFrame;

    private bool estaMorto;
    private float timerAtaqueAtivo;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResolverTodosClipSprites();
        TocarClipe(idle, true);
    }

    void Update()
    {
        if (timerAtaqueAtivo > 0f && !estaMorto)
        {
            timerAtaqueAtivo -= Time.deltaTime;
        }

        AtualizarFrames();
    }

    private void ResolverTodosClipSprites()
    {
        idle.frames = idle.ObterFramesResolvidos();
        walk.frames = walk.ObterFramesResolvidos();
        jump.frames = jump.ObterFramesResolvidos();
        death.frames = death.ObterFramesResolvidos();
        sword.frames = sword.ObterFramesResolvidos();
        spear.frames = spear.ObterFramesResolvidos();
        axe.frames = axe.ObterFramesResolvidos();
        gun.frames = gun.ObterFramesResolvidos();
        pogo.frames = pogo.ObterFramesResolvidos();
    }

    public bool EstaMorto()
    {
        return estaMorto;
    }

    public void AtualizarLocomocao(float velocidadeHorizontal, bool estaNoChao)
    {
        if (estaMorto || timerAtaqueAtivo > 0f) return;

        if (!estaNoChao)
        {
            TocarClipe(jump, false);
            return;
        }

        if (Mathf.Abs(velocidadeHorizontal) > 0.1f)
        {
            TocarClipe(walk, false);
        }
        else
        {
            TocarClipe(idle, false);
        }
    }

    public void ForcarPulo()
    {
        if (estaMorto || timerAtaqueAtivo > 0f) return;
        TocarClipe(jump, true);
    }

    public void TocarAtaque(int armaId, bool pogoAtivo)
    {
        if (estaMorto) return;

        ClipeSprite escolhido = sword;

        if (pogoAtivo)
        {
            escolhido = pogo;
        }
        else
        {
            switch (armaId)
            {
                case 1:
                    escolhido = sword;
                    break;
                case 2:
                    escolhido = spear;
                    break;
                case 3:
                    escolhido = axe;
                    break;
                case 4:
                    escolhido = gun;
                    break;
            }
        }

        TocarClipe(escolhido, true);
        timerAtaqueAtivo = CalcularDuracao(escolhido);
    }

    public void TocarMorte()
    {
        if (estaMorto) return;
        estaMorto = true;
        timerAtaqueAtivo = 0f;
        TocarClipe(death, true);
    }

    public float ObterDuracaoMorte()
    {
        return Mathf.Max(0.2f, CalcularDuracao(death));
    }

    private float CalcularDuracao(ClipeSprite clipe)
    {
        Sprite[] lista = clipe.frames ?? clipe.ObterFramesResolvidos();
        if (lista == null || lista.Length == 0) return 0.25f;
        return lista.Length / Mathf.Max(0.1f, clipe.fps);
    }

    private void TocarClipe(ClipeSprite clipe, bool reiniciar)
    {
        string chave = string.IsNullOrEmpty(clipe.nome) ? "clip" : clipe.nome;
        if (!reiniciar && nomeAtual == chave) return;

        nomeAtual = chave;
        clipeAtual = clipe;
        framesAtuais = clipe.frames != null && clipe.frames.Length > 0 ? clipe.frames : clipe.ObterFramesResolvidos();
        indiceFrame = 0;
        timerFrame = 0f;

        if (spriteRenderer != null && framesAtuais != null && framesAtuais.Length > 0 && framesAtuais[0] != null)
        {
            spriteRenderer.sprite = framesAtuais[0];
        }
    }

    private void AtualizarFrames()
    {
        if (spriteRenderer == null || framesAtuais == null || framesAtuais.Length == 0) return;

        float intervalo = 1f / Mathf.Max(0.1f, clipeAtual.fps);
        timerFrame += Time.deltaTime;

        while (timerFrame >= intervalo)
        {
            timerFrame -= intervalo;
            indiceFrame++;

            if (indiceFrame >= framesAtuais.Length)
            {
                if (clipeAtual.loop)
                {
                    indiceFrame = 0;
                }
                else
                {
                    indiceFrame = framesAtuais.Length - 1;
                    timerFrame = 0f;
                    break;
                }
            }

            if (framesAtuais[indiceFrame] != null)
            {
                spriteRenderer.sprite = framesAtuais[indiceFrame];
            }
        }
    }
}