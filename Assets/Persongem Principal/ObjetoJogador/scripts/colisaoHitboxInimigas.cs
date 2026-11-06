using UnityEngine;

public class colisaoHitboxInimigas : MonoBehaviour
{
    [Header("Propriedades deste Inimigo")]
    [Tooltip("Defina o dano específico para este tipo de ataque/inimigo")]
    public int danoDoAtaque = 15; 
    
    public LayerMask layerDoJogador;
    public Vector2 tamanhoHitbox = new Vector2(1f, 1f);
    public Vector2 offsetHitbox = Vector2.zero;

    private bool jaAcertou = false;

    void Update()
    {
        Vector2 posicaoAtual = (Vector2)transform.position + offsetHitbox;
        Collider2D hitJogador = Physics2D.OverlapBox(posicaoAtual, tamanhoHitbox, 0f, layerDoJogador);

        if (hitJogador != null && !jaAcertou)
        {
            combate jogadorCombate = hitJogador.GetComponent<combate>();
            
            if (jogadorCombate != null)
            {
                // Envia o dano configurado neste inimigo para o jogador
                jogadorCombate.TomarDano(danoDoAtaque);
                jaAcertou = true; 
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 posicaoGizmo = transform.position + (Vector3)offsetHitbox;
        Gizmos.DrawWireCube(posicaoGizmo, new Vector3(tamanhoHitbox.x, tamanhoHitbox.y, 1f));
    }
}