using UnityEngine;

public class aplicarRespawn : MonoBehaviour
{
    void Start()
    {
        Debug.Log(">>> [RESPAWN] aplicarRespawn.Start() executado no objeto: " + gameObject.name);
        Vector3? posPendente = GerenciadorCheckpoint.ObterPosicaoPendente();
        if (posPendente.HasValue)
        {
            Debug.Log(">>> [RESPAWN] Posição pendente encontrada: " + posPendente.Value + " | Posição atual do jogador antes do ajuste: " + transform.position);
            transform.position = posPendente.Value;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = posPendente.Value;
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log(">>> [RESPAWN] Posição aplicada com sucesso! Nova posição do jogador: " + transform.position);
        }
        else
        {
            Debug.Log(">>> [RESPAWN] Nenhuma posição de respawn pendente encontrada.");
        }
    }
}
