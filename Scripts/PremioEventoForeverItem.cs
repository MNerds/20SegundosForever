using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class PremioEventoForeverItem : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text posicionText;
    [SerializeField] private TMP_Text nombreText;
    [SerializeField] private TMP_Text descripcionText;
    [SerializeField] private TMP_Text precioText;

    [Header("Imagen Premio")]
    [SerializeField] private Image premioImage;

    [Header("Fondo por Posicion")]
    [SerializeField] private Image fondoImage;
    [SerializeField] private Sprite spriteTop3;
    [SerializeField] private Sprite spriteNormal;

    private PriceEvent premioData;
    private int index;

    public async Task ConfigurarAsync(PriceEvent premio, int index)
    {
        this.premioData = premio;
        this.index = index;

        int posicion = index + 1;

        if (posicionText != null)
            posicionText.text = posicion.ToString();

        if (nombreText != null)
            nombreText.text = premio.DescriptionNormal;

        if (descripcionText != null)
            descripcionText.text = premio.DescriptionNormal;

        if (precioText != null)
            precioText.text = premio.priceNormal?.ToString();

        if (premioImage != null)
            premioImage.sprite = await ImageCacheManager.GetRemoteTexture(premio.ImageNormal);
        

        ConfigurarFondo(posicion);
    }

    private void ConfigurarFondo(int posicion)
    {
        if (fondoImage == null)
            return;

        if (posicion <= 3)
        {
            if (spriteTop3 != null)
                fondoImage.sprite = spriteTop3;
        }
        else
        {
            if (spriteNormal != null)
                fondoImage.sprite = spriteNormal;
        }
    }

    public PriceEvent GetPremioData()
    {
        return premioData;
    }

    public int GetIndex()
    {
        return index;
    }
}