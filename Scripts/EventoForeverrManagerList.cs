using UnityEngine;

public class EventoForeverrManagerList : MonoBehaviour
{
    [Header("Prefabs & UI")]
    [SerializeField] private Transform tematicasContent; // panel con VerticalLayoutGroup
    [SerializeField] private EventoForeverItem eventoPrefab;
    private void OnEnable()
    {
        CargarTematicas(TematicasLoader.tematicasCategoryRootResponse);
    }
    public void CargarTematicas(TematicasCategoryRootResponse response)
    {
        if (response == null || response.data == null || response.data.tematica == null)
            return;
        Debug.Log("****tllegue a ca ");
        // Limpia hijos previos si querés refrescar
        foreach (Transform child in tematicasContent)
        {
            Destroy(child.gameObject);
        }

        // Instancia cada tematica
        foreach (var t in response.data.tematica)
        {
            Debug.Log("****t " + t.name + " -* " + t.id);

            EventoForeverItem item = Instantiate(eventoPrefab, tematicasContent);
            _ = item.SetDataAsync(t); // usa el método del prefab para setear datos
        }
    }
}
