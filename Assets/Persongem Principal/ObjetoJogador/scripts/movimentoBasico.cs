using UnityEngine;

public class movimentoBasico : MonoBehaviour
{
    private float horizontal;
    public float speed;
    public float jumpingPower;
    private bool isFacingRight = true;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public Transform groundCheck;
    [SerializeField] public LayerMask groundLayer;

    // Variável para congelar temporariamente o movimento durante o knockback
    private float tempoIgnorarMovimento = 0f;

    void Start()
    {
        
    }

    void Update()
    {
        // Se estiver sob efeito de knockback, diminui o tempo de bloqueio
        if (tempoIgnorarMovimento > 0f)
        {
            tempoIgnorarMovimento -= Time.deltaTime;
            return; // Ignora o input normal enquanto sofre o recuo
        }

        horizontal = Input.GetAxisRaw("Horizontal");

        Flip();

        if (Input.GetButtonDown("Jump") && IsGrounded()){
            rb.linearVelocity = new Vector2(rb.linearVelocity.x , jumpingPower);
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f){
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void FixedUpdate(){
        // Se estiver em knockback, deixa a física livre (não sobrescreve o velocity)
        if (tempoIgnorarMovimento > 0f) return;

        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    private void Flip(){
        if ((isFacingRight && horizontal < 0f) || (!isFacingRight && horizontal > 0f)){
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded(){
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    // Método público chamado pelo combate.cs para aplicar o recuo e bloquear o input por um instante
    public void AplicarForcaKnockback(Vector2 direcaoForca, float tempoDuracao)
    {
        rb.linearVelocity = direcaoForca;
        tempoIgnorarMovimento = tempoDuracao;
    }

    public bool EstaNoChao()
    {
        return IsGrounded();
    }

    public bool EstaViradoDireita()
    {
        return isFacingRight;
    }
}