using UnityEngine;

public class inimigoScript : MonoBehaviour
{
    [Header("Atributos de Vida")]
    public int vidaInimigo = 20; 
    public float tempoDeInvencibilidadeInimigo = 0.2f; 
    private float contadorInvencibilidadeInimigo = 0f;

    [Header("Drop de Item")]
    [Tooltip("Prefab do item que será dropado quando o inimigo morrer.")]
    public GameObject prefabItemDrop;
    [Range(0f, 100f)]
    [Tooltip("Chance em porcentagem de dropar o item (100 = sempre dropa se houver prefab).")]
    public float chanceDropPorcentagem = 100f;
    [Tooltip("Ponto opcional para nascer o item. Se vazio, usa a posição do inimigo.")]
    public Transform pontoDrop;
    [Tooltip("Offset aplicado na posição de drop do item.")]
    public Vector2 offsetDrop = Vector2.zero;

    private bool itemJaDropado = false;

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
            scriptCobra cobra = GetComponent<scriptCobra>();
            if (cobra != null && cobra.IniciarAnimacaoDeath())
            {
                DroparItem();
                return;
            }
            scriptUrubu urubu = GetComponent<scriptUrubu>();
            if (urubu != null && urubu.IniciarAnimacaoMorte())
            {
                DroparItem();
                return;
            }
            mumia mumiaInimiga = GetComponent<mumia>();
            if (mumiaInimiga != null && mumiaInimiga.IniciarAnimacaoMorte())
            {
                DroparItem();
                return;
            }
            sacerdote sacerdoteInimigo = GetComponent<sacerdote>();
            if (sacerdoteInimigo != null && sacerdoteInimigo.IniciarAnimacaoMorte())
            {
                DroparItem();
                return;
            }
            kingOfDesert rei = GetComponent<kingOfDesert>();
            if (rei != null && rei.IniciarAnimacaoMorte())
            {
                DroparItem();
                return;
            }
            Morrer();
            return;
        }

        scriptCobra cobraAtingida = GetComponent<scriptCobra>();
        if (cobraAtingida != null) cobraAtingida.ReproduzirAnimacaoHurt();
    }

    public void DroparItem()
    {
        if (itemJaDropado || prefabItemDrop == null) return;

        if (chanceDropPorcentagem < 100f)
        {
            float sorteio = Random.Range(0f, 100f);
            if (sorteio > chanceDropPorcentagem)
            {
                itemJaDropado = true;
                return;
            }
        }

        Vector3 basePos = pontoDrop != null ? pontoDrop.position : transform.position;
        Vector3 spawnPos = new Vector3(
            basePos.x + offsetDrop.x,
            basePos.y + offsetDrop.y,
            transform.position.z
        );

        Instantiate(prefabItemDrop, spawnPos, Quaternion.identity);
        itemJaDropado = true;
    }

    void Morrer()
    {
        print("Inimigo morreu!");
        DroparItem();
        Destroy(gameObject); 
    }
}