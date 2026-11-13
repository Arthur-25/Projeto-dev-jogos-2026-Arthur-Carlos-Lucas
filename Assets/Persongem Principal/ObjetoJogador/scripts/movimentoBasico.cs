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

    private Collider2D col;

    // Variável para congelar temporariamente o movimento durante o knockback
    private float tempoIgnorarMovimento = 0f;

    void Start()
    {
        col = GetComponent<Collider2D>();
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

        float moveDir = horizontal;

        // Se estiver no ar e empurrando contra uma parede lateral verdadeira, zera o movimento para não grudar
        if (moveDir != 0 && !IsGrounded() && IsTouchingWall(moveDir))
        {
            moveDir = 0f;
        }

        rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);
    }

    private bool IsTouchingWall(float dir)
    {
        if (col == null) return false;

        float rayLen = 0.12f;
        Vector2 origin = new Vector2(col.bounds.center.x + (dir > 0 ? col.bounds.extents.x : -col.bounds.extents.x), col.bounds.center.y);
        
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dir, 0), rayLen, groundLayer);
        if (hit.collider == null)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, new Vector2(dir, 0), rayLen);
            foreach (var h in hits)
            {
                if (h.collider != col && !h.collider.isTrigger)
                {
                    hit = h;
                    break;
                }
            }
        }

        return hit.collider != null && Mathf.Abs(hit.normal.x) > 0.8f;
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
        if (col != null)
        {
            float rayLength = 0.25f;
            Vector2 leftPos = new Vector2(col.bounds.min.x + 0.05f, col.bounds.min.y);
            Vector2 centerPos = new Vector2(col.bounds.center.x, col.bounds.min.y);
            Vector2 rightPos = new Vector2(col.bounds.max.x - 0.05f, col.bounds.min.y);

            RaycastHit2D hitLeft = Physics2D.Raycast(leftPos, Vector2.down, rayLength, groundLayer);
            RaycastHit2D hitCenter = Physics2D.Raycast(centerPos, Vector2.down, rayLength, groundLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(rightPos, Vector2.down, rayLength, groundLayer);

            // Garante que o que foi atingido é realmente o chão (normal apontando para cima, y > 0.7 e x baixo), e não uma parede lateral
            bool groundedLeft = hitLeft.collider != null && hitLeft.normal.y > 0.7f && Mathf.Abs(hitLeft.normal.x) < 0.5f;
            bool groundedCenter = hitCenter.collider != null && hitCenter.normal.y > 0.7f && Mathf.Abs(hitCenter.normal.x) < 0.5f;
            bool groundedRight = hitRight.collider != null && hitRight.normal.y > 0.7f && Mathf.Abs(hitRight.normal.x) < 0.5f;

            return groundedLeft || groundedCenter || groundedRight;
        }
        else if (groundCheck != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.25f, groundLayer);
            return hit.collider != null && hit.normal.y > 0.7f && Mathf.Abs(hit.normal.x) < 0.5f;
        }
        return false;
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
