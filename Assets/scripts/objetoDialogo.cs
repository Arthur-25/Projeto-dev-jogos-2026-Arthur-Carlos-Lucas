using UnityEngine;

[System.Serializable]

public class objetoDialogo 
{
    public string texto;
    public string locutor;

    public objetoDialogo(string txt, string loc){
        this.texto = txt;
        this.locutor = loc;
    }

}
