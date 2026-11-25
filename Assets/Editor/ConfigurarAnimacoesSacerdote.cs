#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ConfigurarAnimacoesSacerdote
{
    private const string PastaBase = "Assets/Inimigos/Inimigos pro Deserto/6 Deceased/";
    private const string PastaAnimator = PastaBase + "animator/";
    private const string PastaClipsGerados = PastaAnimator + "GeneratedClips/";

    [MenuItem("Tools/Sacerdote/Recriar Animacoes dos Spritesheets")]
    private static void RecriarAnimacoes()
    {
        if (!AssetDatabase.IsValidFolder(PastaClipsGerados))
        {
            AssetDatabase.CreateFolder(PastaAnimator, "GeneratedClips");
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PastaAnimator + "sacerdote.controller");
        if (controller == null || controller.layers.Length == 0)
        {
            Debug.LogError("O controller sacerdote.controller nao foi encontrado ou nao tem Base Layer.");
            return;
        }

        AnimationClip idle = CriarClip("idle", "Deceased_idle.png", "Deceased_idle_", true, 6f);
        AnimationClip walk = CriarClip("walk", "Deceased_walk.png", "Deceased_walk_", true, 10f);
        AnimationClip ataque = CriarClip("ataque", "Deceased_attack.png", "Deceased_attack_", false, 12f);
        AnimationClip die = CriarClip("die", "Deceased_death.png", "Deceased_death_", false, 12f);

        if (idle == null || walk == null || ataque == null || die == null) return;

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        bool estadosEncontrados =
            AtribuirMotion(maquina, "idle", idle) &&
            AtribuirMotion(maquina, "walk", walk) &&
            AtribuirMotion(maquina, "ataque", ataque) &&
            (AtribuirMotion(maquina, "die", die) || AtribuirMotion(maquina, "death", die));

        if (!estadosEncontrados)
        {
            Debug.LogError("Confira se o Animator tem os estados idle, walk, ataque e die/death.");
            return;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = controller;
        Debug.Log("Animacoes do sacerdote geradas a partir dos spritesheets e conectadas ao controller.");
    }

    private static AnimationClip CriarClip(
        string nomeClip,
        string nomeSpritesheet,
        string prefixoSprite,
        bool loop,
        float quadrosPorSegundo)
    {
        string caminhoSpritesheet = PastaBase + nomeSpritesheet;
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(caminhoSpritesheet)
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith(prefixoSprite))
            .OrderBy(sprite => NumeroFinal(sprite.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"Nenhum sprite fatiado encontrado em {caminhoSpritesheet}.");
            return null;
        }

        string caminhoClip = PastaClipsGerados + nomeClip + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminhoClip);
        if (clip == null)
        {
            clip = new AnimationClip { name = nomeClip };
            AssetDatabase.CreateAsset(clip, caminhoClip);
        }
        clip.name = nomeClip;
        clip.frameRate = quadrosPorSegundo;
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty, typeof(SpriteRenderer), "m_Sprite");

        var keyframes = new List<ObjectReferenceKeyframe>();
        for (int index = 0; index < sprites.Length; index++)
        {
            keyframes.Add(new ObjectReferenceKeyframe
            {
                time = index / quadrosPorSegundo,
                value = sprites[index]
            });
        }

        keyframes.Add(new ObjectReferenceKeyframe
        {
            time = sprites.Length / quadrosPorSegundo,
            value = sprites[sprites.Length - 1]
        });

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static int NumeroFinal(string nome)
    {
        int separador = nome.LastIndexOf('_');
        return separador >= 0 && int.TryParse(nome.Substring(separador + 1), out int numero)
            ? numero
            : 0;
    }

    private static bool AtribuirMotion(AnimatorStateMachine maquina, string nomeEstado, Motion motion)
    {
        foreach (ChildAnimatorState estado in maquina.states)
        {
            if (estado.state.name != nomeEstado) continue;
            estado.state.motion = motion;
            EditorUtility.SetDirty(estado.state);
            return true;
        }

        return false;
    }
}
#endif
