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
            Morrer();
        }
    }

    void Morrer()
    {
        print("Inimigo morreu!");
        Destroy(gameObject); 
    }
}