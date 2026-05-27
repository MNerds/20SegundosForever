using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PremioEventoForeverItem : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text nombreText;
    [SerializeField] private TMP_Text descripcionText;
    [SerializeField] private TMP_Text precioText;

    [Header("Imagen")]
    [SerializeField] private Image premioImage;

    private PriceEvent premioData;
    private int index;

    public void Configurar(PriceEvent premio, int index)
    {
        this.premioData = premio;
        this.index = index;

        if (nombreText != null)
            nombreText.text = premio.nameNormal;

        if (descripcionText != null)
            descripcionText.text = premio.descriptionNormal;

        if (precioText != null)
            precioText.text = premio.priceNormal.ToString();

        if (premioImage != null)
            premioImage.sprite = null;

        // Si después querés cargar imageNormal desde URL,
        // acá se puede agregar una corrutina con UnityWebRequestTexture.
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