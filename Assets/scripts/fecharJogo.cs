using UnityEngine;

public class fecharJogo : MonoBehaviour
{
    public void Fechar()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}