using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class checkpoint : MonoBehaviour
{
    [Header("Configuração")]
    public float distanciaInteracao = 2f;
    public Transform pontoSpawn; // Ponto exato onde o jogador vai renascer (opcional)
    private Transform jogadorTransform;
    private bool jogadorNaArea = false;

    [Header("Feedback Visual (Opcional)")]
    public SpriteRenderer spriteRenderer;
    public Sprite spriteAtivo;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jogadorTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (jogadorTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) jogadorTransform = playerObj.transform;
            return;
        }

        float distancia = Vector2.Distance(transform.position, jogadorTransform.position);
        jogadorNaArea = (distancia <= distanciaInteracao);

        if (jogadorNaArea)
        {
            bool apertouEnter = (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.Return);

            if (apertouEnter)
            {
                AtivarCheckpoint();
            }
        }
    }

    void AtivarCheckpoint()
    {
        Vector3 posSalvar = pontoSpawn != null ? pontoSpawn.position : transform.position;
        string cenaAtual = SceneManager.GetActiveScene().name;

        GerenciadorCheckpoint.Salvar(cenaAtual, posSalvar);

        if (spriteRenderer != null && spriteAtivo != null)
        {
            spriteRenderer.sprite = spriteAtivo;
        }

        Debug.Log("CHECKPOINT ATIVADO E SALVO COM SUCESSO!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}
