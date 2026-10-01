using UnityEngine;

public class inimigoScript : MonoBehaviour
{
    private float velocidade = 2f;
    private bool moveDireita = true; 

    public LayerMask layerDoChao;
    public float distanciaDoRaio = 0.3f; 
    public float deslocamentoFrente = 0.4f; 
    private bool estaNoChao;

    // Variáveis de vida e i-frames do inimigo
    public int vidaInimigo = 20; 
    public float tempoDeInvencibilidadeInimigo = 0.2f; // Tempo que ele fica imune após tomar um golpe
    private float contadorInvencibilidadeInimigo = 0f;

    public void TomarDano(int quantidadeDano)
    {
        // Se ainda estiver no tempo de invencibilidade, ignora o dano
        if (contadorInvencibilidadeInimigo > 0f) return;

        vidaInimigo -= quantidadeDano;
        print("Inimigo sofreu dano! Vida restante: " + vidaInimigo);

        // Ativa os i-frames após tomar o golpe
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

    void Update()
    {
        // Atualiza o contador de i-frames do inimigo
        if (contadorInvencibilidadeInimigo > 0f)
        {
            contadorInvencibilidadeInimigo -= Time.deltaTime;
        }

        Vector2 posicaoBase = transform.position;
        posicaoBase.y -= 1.0f; 

        if (moveDireita)
        {
            posicaoBase.x += deslocamentoFrente;
        }
        else
        {
            posicaoBase.x -= deslocamentoFrente;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(posicaoBase, Vector2.down, distanciaDoRaio, layerDoChao);
        
        estaNoChao = false;
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject != gameObject)
            {
                estaNoChao = true; 
                break;
            }
        }

        if (!estaNoChao)
        {
            moveDireita = !moveDireita; 
        }

        float direcaoX = moveDireita ? 1f : -1f;

        Vector3 novaPosicao = transform.position;
        novaPosicao.x += velocidade * direcaoX * Time.deltaTime;

        transform.position = novaPosicao;
    }

    private void OnDrawGizmos()
    {
        Vector2 posicaoBase = transform.position;
        posicaoBase.y -= 1.0f; 

        if (Application.isPlaying)
        {
            if (moveDireita) posicaoBase.x += deslocamentoFrente;
            else posicaoBase.x -= deslocamentoFrente;
        }
        else
        {
            posicaoBase.x += deslocamentoFrente;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(posicaoBase, posicaoBase + (Vector2.down * distanciaDoRaio));
    }
}