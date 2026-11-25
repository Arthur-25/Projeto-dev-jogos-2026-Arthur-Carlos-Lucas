#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RecriarAnimatorUrubu
{
    private const string PastaAnimacoes = "Assets/Inimigos/Inimigos pro Deserto/4 Vulture/animator/";

    [MenuItem("Tools/Urubu/Criar Animator de Recuperacao")]
    private static void CriarAnimator()
    {
        string caminhoController = AssetDatabase.GenerateUniqueAssetPath(
            PastaAnimacoes + "urubu_recovery.controller");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(caminhoController);
        AnimatorStateMachine maquina = controller.layers[0].stateMachine;

        AnimatorState idle = AdicionarEstado(maquina, "idle", "idle.anim");
        AdicionarEstado(maquina, "walk", "walk.anim");
        AdicionarEstado(maquina, "attack", "attack.anim");
        AdicionarEstado(maquina, "death", "death.anim");
        maquina.defaultState = idle;

        Animator selecionado = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Animator>()
            : null;

        if (selecionado != null)
        {
            Undo.RecordObject(selecionado, "Assign Urubu Animator Controller");
            selecionado.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(selecionado);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;

        Debug.Log(selecionado != null
            ? $"Controller criado e atribuído: {caminhoController}"
            : $"Controller criado: {caminhoController}. Selecione o urubu e atribua este asset no componente Animator.");
    }

    private static AnimatorState AdicionarEstado(
        AnimatorStateMachine maquina,
        string nomeEstado,
        string nomeClip)
    {
        AnimatorState estado = maquina.AddState(nomeEstado);
        string caminhoClip = PastaAnimacoes + nomeClip;
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminhoClip);

        if (clip == null)
        {
            Debug.LogWarning($"Não foi possível carregar o clip: {caminhoClip}");
        }
        else
        {
            estado.motion = clip;
        }

        return estado;
    }
}
#endif