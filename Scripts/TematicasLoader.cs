using System;
using System.Threading.Tasks;
using UnityEngine;

public class TematicasLoader : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject tematicaPrefab;
    [SerializeField] private Transform scrollPanel;
    private bool firstTime = true;
    private void OnEnable()
    {
        LimpiarPanel();
        LoadTematicas();
    }

    private async void LoadTematicas()
    {
        var _result = await MySqlManager.GetTematicasForever(false);

        if (_result.success)
        {
            CargarTematicas(_result.value);
            GetComponentInChildren<SnapScrollRect>().StartScroll();
        }
        else
        { 
            Debug.Log("Error al cargar las tematicas: " + _result.error);
        }
    }


    public void CargarTematicas(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogWarning("JSON vacío.");
            return;
        }

        RootResponse response = JsonUtility.FromJson<RootResponse>(jsonData);

        Array.Sort(response.data.tematica, (a, b) =>
        {
            DateTime fechaA = DateTime.Parse(a.fechaOn);
            DateTime fechaB = DateTime.Parse(b.fechaOn);
            return fechaA.CompareTo(fechaB);
        });
        bool _firstItem = true;
        foreach (TematicaData tematica in response.data.tematica)
        {
            GameObject item = Instantiate(tematicaPrefab, scrollPanel);

            TematicaItem uiItem = item.GetComponent<TematicaItem>();
            if(_firstItem)
            {
                tematica.is20Seg = true;//null para 20 segundos
                _firstItem = false;
            }
            if (uiItem != null)
            {
                uiItem.SetTematica(
                    tematica.id,
                    tematica.name,
                    tematica.desc,
                    tematica.urlImg,
                    tematica.urlImgBanner,
                    tematica.status,
                    tematica.is20Seg,
                    firstTime
                );
                firstTime = false;
            }
        }
    }

        private void LimpiarPanel()
    {
        for (int i = scrollPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(scrollPanel.GetChild(i).gameObject);
        }
    }
}

[Serializable]
public class RootResponse
{
    public string status;
    public string message;
    public ResponseData data;
}

[Serializable]
public class ResponseData
{
    public TematicaData[] tematica;
    public CategoriaData[] categorias;
}

[Serializable]
public class TematicaData
{
    public string id;
    public string name;
    public string desc;
    public string urlImgBanner;
    public string urlImg;
    public string fechaOn;
    public string fechaOff;
    public string status;
    public bool is20Seg;
}

[Serializable]
public class CategoriaData
{
    public string id;
    public string name;
    public string desc;
    public string urlImg;
    public int status;
}