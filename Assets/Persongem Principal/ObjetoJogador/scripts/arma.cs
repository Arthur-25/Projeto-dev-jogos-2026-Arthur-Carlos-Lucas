using UnityEngine;

[System.Serializable]
public class arma 
{
    public int ID;
    public float ataque;
    public float alcance;
    public float velocidade;
    public Vector2 tamanhoHitboxArmaPoder;
    public Vector2 tamanhoHitboxArmaPrecisao;

    public float tempoDuracaoAtaque;
    public float tempoCooldown;
    public float forcaKnockbackX;
    public float forcaKnockbackY;

    public arma(int id, float atq, float alc, float vel, Vector2 hitboxArmPoder, Vector2 hitboxArmPrec, float duracaoAtaque, float cooldown, float kbX, float kbY)
    {
        this.ID = id;
        this.ataque = atq;
        this.alcance = alc;
        this.velocidade = vel;
        this.tamanhoHitboxArmaPoder = hitboxArmPoder;
        this.tamanhoHitboxArmaPrecisao = hitboxArmPrec;
        this.tempoDuracaoAtaque = duracaoAtaque;
        this.tempoCooldown = cooldown;
        this.forcaKnockbackX = kbX;
        this.forcaKnockbackY = kbY;
    }
}