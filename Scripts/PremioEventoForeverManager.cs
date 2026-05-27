using UnityEngine;

public class PremioEventoForeverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private PremioEventoForeverItem premioPrefab;

    [Header("Opciones")]
    [SerializeField] private bool cargarAlIniciar = true;
    [SerializeField] private bool limpiarAntesDeCargar = true;

    private void Start()
    {
        if (cargarAlIniciar)
        {
            CargarPremios();
        }
    }

    public void CargarPremios()
    {
        if (content == null)
        {
            Debug.LogError("[PremioEventoForeverManager] No se asignó el Content.");
            return;
        }

        if (premioPrefab == null)
        {
            Debug.LogError("[PremioEventoForeverManager] No se asignó el prefab PremioEventoForeverItem.");
            return;
        }

        if (limpiarAntesDeCargar)
        {
            LimpiarContent();
        }

        PriceEvent[] premios = GamePlay20SegForever.TEMATICA_PREMIOS;

        if (premios == null || premios.Length == 0)
        {
            Debug.LogWarning("[PremioEventoForeverManager] No hay premios cargados en GamePlay20SegForever.TEMATICA_PREMIOS.");
            return;
        }

        for (int i = 0; i < premios.Length; i++)
        {
            PriceEvent premio = premios[i];

            if (premio == null)
            {
                Debug.LogWarning($"[PremioEventoForeverManager] El premio en índice {i} es null.");
                continue;
            }

            PremioEventoForeverItem item = Instantiate(premioPrefab, content);
            item.transform.SetSiblingIndex(i);

            item.Configurar(premio, i);
        }
    }

    public void RecargarPremios()
    {
        CargarPremios();
    }

    public void LimpiarContent()
    {
        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }
}