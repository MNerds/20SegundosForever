using System;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.U2D.ScriptablePacker;

public class TematicasLoader : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject tematicaPrefab;
    [SerializeField] private Transform scrollPanel;
    public static TematicasCategoryRootResponse tematicasCategoryRootResponse;
    private bool firstTime = true;

    private void OnEnable()
    {
        LimpiarPanel();
        LoadTematicas();
    }

    bool loadingTematicas = false;
    private async void LoadTematicas()
    {
        if (loadingTematicas)
            return;
        while (!gameObject.activeSelf)
        {
            await Task.Delay(10);
        }
        loadingTematicas = true;
        var _result = await MySqlManager.GetTematicasForever(false);

        if (_result.success)
        {
            CargarTematicas(_result.value);

            GetComponentInChildren<SnapScrollRect>(true).StartScroll();
        }
        else
        { 
            Debug.Log("Error al cargar las tematicas: " + _result.error);
        }
        loadingTematicas = false;
    }


    public void CargarTematicas(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogWarning("JSON vacío.");
            return;
        }

        tematicasCategoryRootResponse = JsonUtility.FromJson<TematicasCategoryRootResponse>(jsonData);

        Array.Sort(tematicasCategoryRootResponse.data.tematica, (a, b) =>
        {
            DateTime fechaA = DateTime.Parse(a.fechaOn);
            DateTime fechaB = DateTime.Parse(b.fechaOn);
            return fechaA.CompareTo(fechaB);
        });



        bool _firstItem = true;
        foreach (TematicaData tematica in tematicasCategoryRootResponse.data.tematica)
        {
            if (!tematica.status.Equals("1"))
                continue;
            GameObject item = Instantiate(tematicaPrefab, scrollPanel);

            TematicaItem uiItem = item.GetComponent<TematicaItem>();
            if(_firstItem && tematica.name.ToLower().Equals("20 segundos"))
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
                    tematica.prize,
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
        Debug.Log("***Limpiando panel de tematicas...");
        for (int i = scrollPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(scrollPanel.GetChild(i).gameObject);
        }
    }
}

[Serializable]
public class TematicasCategoryRootResponse
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
    public PriceEvent[] prize;
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