using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class dialogoNpc : MonoBehaviour
{
    public objetoDialogo[] vetorDialogosNpc;

    [Header("Audio")]
    public AudioSource fonteAudio;
    public bool pararAudioAoEncerrar = true;

    private int indiceAtual = -1;

    void Start()
    {
        if (fonteAudio == null)
        {
            fonteAudio = GetComponent<AudioSource>();
        }

        if (fonteAudio != null)
        {
            fonteAudio.playOnAwake = false;
        }
    }

    public objetoDialogo ObterDialogoAtual()
    {
        if (vetorDialogosNpc == null || indiceAtual < 0 || indiceAtual >= vetorDialogosNpc.Length)
        {
            return null;
        }

        return vetorDialogosNpc[indiceAtual];
    }

    public void IniciarDialogo(int indiceInicial = 0)
    {
        if (vetorDialogosNpc == null || vetorDialogosNpc.Length == 0) return;

        indiceAtual = Mathf.Clamp(indiceInicial, 0, vetorDialogosNpc.Length - 1);
        AplicarAudioDoDialogo(vetorDialogosNpc[indiceAtual]);
    }

    public bool AvancarDialogo()
    {
        if (vetorDialogosNpc == null || vetorDialogosNpc.Length == 0) return false;

        int proximoIndice = indiceAtual + 1;
        if (proximoIndice >= vetorDialogosNpc.Length)
        {
            EncerrarDialogo();
            return false;
        }

        indiceAtual = proximoIndice;
        AplicarAudioDoDialogo(vetorDialogosNpc[indiceAtual]);
        return true;
    }

    public void IrParaDialogo(int indice)
    {
        if (vetorDialogosNpc == null || indice < 0 || indice >= vetorDialogosNpc.Length) return;

        indiceAtual = indice;
        AplicarAudioDoDialogo(vetorDialogosNpc[indiceAtual]);
    }

    public void EncerrarDialogo()
    {
        indiceAtual = -1;
        if (pararAudioAoEncerrar && fonteAudio != null)
        {
            fonteAudio.Stop();
            fonteAudio.loop = false;
            fonteAudio.clip = null;
        }
    }

    private void AplicarAudioDoDialogo(objetoDialogo dialogo)
    {
        if (fonteAudio == null) return;

        fonteAudio.Stop();

        if (dialogo != null && dialogo.audioClip != null)
        {
            fonteAudio.clip = dialogo.audioClip;
            fonteAudio.loop = dialogo.loopAudio;
            fonteAudio.Play();
        }
        else
        {
            fonteAudio.loop = false;
            fonteAudio.clip = null;
        }
    }
}