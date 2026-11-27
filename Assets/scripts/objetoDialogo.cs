using UnityEngine;

[System.Serializable]
public class objetoDialogo
{
    public string texto;
    public string locutor;
    public Sprite icone;
    public AudioClip audioClip;
    public bool loopAudio;

    public objetoDialogo(string txt, string loc, Sprite ico = null, AudioClip clip = null, bool loop = false)
    {
        this.texto = txt;
        this.locutor = loc;
        this.icone = ico;
        this.audioClip = clip;
        this.loopAudio = loop;
    }
}