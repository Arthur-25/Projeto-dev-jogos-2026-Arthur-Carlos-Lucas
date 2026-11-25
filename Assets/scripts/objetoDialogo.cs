using UnityEngine;

[System.Serializable]

public class objetoDialogo 
{
    public string texto;
    public string locutor;
    public Sprite icone;

    public objetoDialogo(string txt, string loc, Sprite ico = null){
        this.texto = txt;
        this.locutor = loc;
        this.icone = ico;
    }

}
