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
    private Animator animator;
    private string animacaoAtual;

    // Variavel para congelar temporariamente o movimento durante o knockback
    private float tempoIgnorarMovimento = 0f;
    private float bloqueioAnimacaoAtaque = 0f;
    private bool estaMorto = false;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            AvisarSeEstadoAusente("idle");
            AvisarSeEstadoAusente("walk");
            AvisarSeEstadoAusente("jump");
            AvisarSeEstadoAusente("death");
        }

        TocarAnimacao("idle");
    }

    void Update()
    {
        if (estaMorto)
        {
            horizontal = 0f;
            return;
        }

        if (bloqueioAnimacaoAtaque > 0f)
        {
            bloqueioAnimacaoAtaque -= Time.deltaTime;
        }

        // Se estiver sob efeito de knockback, diminui o tempo de bloqueio
        if (tempoIgnorarMovimento > 0f)
        {
            tempoIgnorarMovimento -= Time.deltaTime;
            AtualizarAnimacaoMovimento();
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");

        Flip();

        bool noChao = IsGrounded();

        if (Input.GetButtonDown("Jump") && noChao)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            if (bloqueioAnimacaoAtaque <= 0f)
            {
                TocarAnimacao("jump", true);
            }
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        AtualizarAnimacaoMovimento(noChao);
    }

    private void FixedUpdate()
    {
        if (estaMorto)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Se estiver em knockback, deixa a fisica livre (nao sobrescreve o velocity)
        if (tempoIgnorarMovimento > 0f) return;

        float moveDir = horizontal;

        // Se estiver no ar e empurrando contra uma parede lateral verdadeira, zera o movimento para nao grudar
        if (moveDir != 0 && !IsGrounded() && IsTouchingWall(moveDir))
        {
            moveDir = 0f;
        }

        rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);
    }

    private void AtualizarAnimacaoMovimento(bool? noChaoOverride = null)
    {
        if (estaMorto || bloqueioAnimacaoAtaque > 0f) return;

        bool noChao = noChaoOverride ?? IsGrounded();
        float velX = rb != null ? rb.linearVelocity.x : horizontal;

        if (!noChao)
        {
            TocarAnimacao("jump");
        }
        else if (Mathf.Abs(velX) > 0.1f)
        {
            TocarAnimacao("walk");
        }
        else
        {
            TocarAnimacao("idle");
        }
    }

    public void BloquearAnimacaoAtaque(float duracao)
    {
        bloqueioAnimacaoAtaque = Mathf.Max(bloqueioAnimacaoAtaque, duracao);
    }

    public void IniciarMorte()
    {
        estaMorto = true;
        horizontal = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        TocarAnimacao("death", true);
    }

    public void TocarAnimacao(string nomeEstado, bool reiniciar = false)
    {
        if (animator == null || (!reiniciar && animacaoAtual == nomeEstado)) return;

        int hashEstado = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hashEstado)) return;

        animator.Play(hashEstado, 0, 0f);
        animacaoAtual = nomeEstado;
    }

    private void AvisarSeEstadoAusente(string nomeEstado)
    {
        if (animator == null) return;
        int hashEstado = Animator.StringToHash("Base Layer." + nomeEstado);
        if (!animator.HasState(0, hashEstado))
        {
            Debug.LogWarning($"{name}: o Animator Controller nao tem o estado 'Base Layer.{nomeEstado}'.", this);
        }
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

    private void Flip()
    {
        if ((isFacingRight && horizontal < 0f) || (!isFacingRight && horizontal > 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded()
    {
        if (col != null)
        {
            float rayLength = 0.25f;
            Vector2 leftPos = new Vector2(col.bounds.min.x + 0.05f, col.bounds.min.y);
            Vector2 centerPos = new Vector2(col.bounds.center.x, col.bounds.min.y);
            Vector2 rightPos = new Vector2(col.bounds.max.x - 0.05f, col.bounds.min.y);

            RaycastHit2D hitLeft = Physics2D.Raycast(leftPos, Vector2.down, rayLength, groundLayer);
            RaycastHit2D hitCenter = Physics2D.Raycast(centerPos, Vector2.down, rayLength, groundLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(rightPos, Vector2.down, rayLength, groundLayer);

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

    // Metodo publico chamado pelo combate.cs para aplicar o recuo e bloquear o input por um instante
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