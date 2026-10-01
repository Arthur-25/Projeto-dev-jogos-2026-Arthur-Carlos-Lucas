using UnityEngine;

public class cameraSeguir : MonoBehaviour
{
    public Transform target;       // O Transform do seu personagem
    public float suavizacao = 5f;  // Velocidade que a câmera acompanha o player
    public Vector3 offset;         // Distância/Deslocamento da câmera em relação ao player (ex: Z = -10)

    void LateUpdate()
    {
        if (target != null)
        {
            // Posição desejada da câmera (mantendo o Z original para não sumir com a cena 2D)
            Vector3 posicaoAlvo = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            // Move a câmera suavemente da posição atual até a posição do alvo
            transform.position = Vector3.Lerp(transform.position, posicaoAlvo, suavizacao * Time.deltaTime);
        }
    }
}
