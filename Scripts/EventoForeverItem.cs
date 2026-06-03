using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventoForeverItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI fechaInicioText;
    [SerializeField] private TextMeshProUGUI fechaFinText;
    [SerializeField] private TextMeshProUGUI nombreText;
    [SerializeField] private Image iconoImage;

    [SerializeField] private string urlImg;

    public async Task SetDataAsync(TematicaData data)
    {
        nombreText.text = data.name;

        // Parse y formateo de fechas
        if (DateTime.TryParse(data.fechaOn, out DateTime fechaOn))
            fechaInicioText.text = fechaOn.ToString("dd/MM/yyyy");
        else
            fechaInicioText.text = data.fechaOn; // fallback

        if (DateTime.TryParse(data.fechaOff, out DateTime fechaOff))
            fechaFinText.text = fechaOff.ToString("dd/MM/yyyy");
        else
            fechaFinText.text = data.fechaOff; // fallback

        urlImg = data.urlImg;

        // Descarga y asignación de imagen
        iconoImage.overrideSprite = await ImageCacheManager.GetRemoteTexture(urlImg);
    }

    public string GetUrlImg()
    {
        return urlImg;
    }
}
