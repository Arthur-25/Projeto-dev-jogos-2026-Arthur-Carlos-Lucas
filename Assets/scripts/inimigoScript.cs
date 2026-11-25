using UnityEngine;

public class inimigoScript : MonoBehaviour
{
    [Header("Atributos de Vida")]
    public int vidaInimigo = 20; 
    public float tempoDeInvencibilidadeInimigo = 0.2f; 
    private float contadorInvencibilidadeInimigo = 0f;

    void Update()
    {
        // Atualiza o contador de i-frames do inimigo
        if (contadorInvencibilidadeInimigo > 0f)
        {
            contadorInvencibilidadeInimigo -= Time.deltaTime;
        }
    }

    public void TomarDano(int quantidadeDano)
    {
        if (contadorInvencibilidadeInimigo > 0f) return;

        vidaInimigo -= quantidadeDano;
        print("Inimigo sofreu dano! Vida restante: " + vidaInimigo);

        contadorInvencibilidadeInimigo = tempoDeInvencibilidadeInimigo;

        if (vidaInimigo <= 0)
        {
            scriptCobra cobra = GetComponent<scriptCobra>();
            if (cobra != null && cobra.IniciarAnimacaoDeath()) return;
            scriptUrubu urubu = GetComponent<scriptUrubu>();
            if (urubu != null && urubu.IniciarAnimacaoMorte()) return;
            mumia mumiaInimiga = GetComponent<mumia>();
            if (mumiaInimiga != null && mumiaInimiga.IniciarAnimacaoMorte()) return;
            sacerdote sacerdoteInimigo = GetComponent<sacerdote>();
            if (sacerdoteInimigo != null && sacerdoteInimigo.IniciarAnimacaoMorte()) return;
            Morrer();
            return;
        }

        scriptCobra cobraAtingida = GetComponent<scriptCobra>();
        if (cobraAtingida != null) cobraAtingida.ReproduzirAnimacaoHurt();
    }

    void Morrer()
    {
        print("Inimigo morreu!");
        Destroy(gameObject); 
    }
}