using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnapScrollRect : MonoBehaviour
{
    [Header("Configuración de UI")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public Button nextBtn;
    public Button prevBtn;

    [Header("Ajustes de Snap")]
    public float snapSpeed = 10f;

    [Header("Fade Banner")]
    public float fadeDistance = 0.25f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Dots / Indicadores")]
    public Transform dotsContent;
    public Sprite dotSprite;
    public Color selectedDotColor = Color.cyan;
    public Color normalDotColor = Color.white;
    public Vector2 dotSize = new Vector2(18f, 18f);

    [Header("Estado")]
    public GameObject currentItem;

    private List<RectTransform> items = new List<RectTransform>();
    private List<Image> dots = new List<Image>();

    private bool isDragging = false;
    private float[] itemPositions;
    private int currentIndex = 0;

    ManagerRankingForever manager;

    private void OnEnable()
    {
        itemPositions = null;
    }

    public void StartScroll()
    {
        // 1. Obtener todos los hijos del content

        items.Clear();
        foreach (RectTransform child in content)
        {
            items.Add(child);
        }

        Adjust();

        // 2. Calcular las posiciones normalizadas (0 a 1) para cada item
        itemPositions = new float[items.Count];
        float distance = 1f / (items.Count - 1);

        for (int i = 0; i < items.Count; i++)
        {
            itemPositions[i] = distance * i;
        }

        CreateDots();

        // 3. Configurar botones
        nextBtn.onClick.AddListener(() => Next());
        prevBtn.onClick.AddListener(() => Previous());

        enabled = true;

        UpdateBannerFade();
        UpdateDots();
    }

    private void OnDisable()
    {
        nextBtn.onClick.RemoveAllListeners();
        prevBtn.onClick.RemoveAllListeners();
    }

    public void ClearChildren(Transform panel)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Destroy(panel.GetChild(i).gameObject);
        }
    }

    private void CreateDots()
    {
        dots.Clear();

        if (dotsContent == null || dotSprite == null)
            return;

        ClearChildren(dotsContent);

        for (int i = 0; i < items.Count; i++)
        {
            GameObject dotObject = new GameObject("Dot_" + i, typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(dotsContent, false);

            RectTransform rectTransform = dotObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = dotSize;

            Image image = dotObject.GetComponent<Image>();
            image.sprite = dotSprite;
            image.color = normalDotColor;

            dots.Add(image);
        }
    }

    private void UpdateDots()
    {
        if (dots == null || dots.Count == 0)
            return;

        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == currentIndex ? selectedDotColor : normalDotColor;
        }
    }

    public void Adjust()
    {
        // 1. Obtenemos el ancho dinámico del ítem
        float itemWidth = content.GetChild(0).GetComponent<RectTransform>().rect.width;

        // 2. Contamos cuántos ítems hay actualmente
        int nItems = content.childCount;

        // 3. Aplicamos tu fórmula: (Ancho * n) + Ancho extra
        // Esto equivale a: itemWidth * (nItems + 1)
        float totalWidth = itemWidth * (nItems + 1);

        // 4. Si usas un HorizontalLayoutGroup, sumamos el espaciado entre ítems
        HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
        {
            totalWidth += layout.spacing * nItems;
            totalWidth += layout.padding.left + layout.padding.right;
        }

        // 5. Aplicamos el tamaño final
        content.sizeDelta = new Vector2(totalWidth, content.sizeDelta.y);
    }

    void Update()
    {
        if (!isDragging && itemPositions != null)
        {
            // Interpolar suavemente hacia la posición del item actual
            float targetPos = itemPositions[currentIndex];

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                targetPos,
                Time.deltaTime * snapSpeed
            );
        }
        else return;

        // Fade de banners según cercanía al centro
        UpdateBannerFade();
    }

    private void UpdateBannerFade()
    {
        if (items == null || items.Count == 0 || itemPositions == null)
            return;

        float currentScrollPos = scrollRect.horizontalNormalizedPosition;

        for (int i = 0; i < items.Count; i++)
        {
            float distanceToCenter = Mathf.Abs(currentScrollPos - itemPositions[i]);

            float t = Mathf.Clamp01(distanceToCenter / fadeDistance);

            // t = 0 cuando está en el centro => alpha 1
            // t = 1 cuando está lejos => alpha 0
            float alpha = fadeCurve.Evaluate(t);

            TematicaItem tematicaItem = items[i].GetComponent<TematicaItem>();

            if (tematicaItem != null)
            {
                tematicaItem.SetBannerAlpha(alpha);
            }
        }
    }

    // Se llama mediante el EventTrigger "EndDrag"
    public void OnEndDrag()
    {
        isDragging = false;

        // Encontrar el item más cercano a la posición actual del scroll
        float currentScrollPos = scrollRect.horizontalNormalizedPosition;
        float closestDist = float.MaxValue;

        for (int i = 0; i < itemPositions.Length; i++)
        {
            float dist = Mathf.Abs(currentScrollPos - itemPositions[i]);

            if (dist < closestDist)
            {
                closestDist = dist;
                currentIndex = i;
            }
        }

        UpdateDots();
    }

    // Se llama mediante el EventTrigger "BeginDrag"
    public void OnBeginDrag()
    {
        isDragging = true;
    }

    public void Next(bool _leaderboard = false)
    {
        if (currentIndex < items.Count - 1)
            currentIndex++;
        UpdateItem(_leaderboard);
    }

    public void Previous(bool _leaderboard = false)
    {
        if (currentIndex > 0)
            currentIndex--;
        UpdateItem(_leaderboard);
    }

    public void UpdateItem(bool _leaderboard)
    {
        // Actualizar el GameObject actual para referencia externa

        currentItem = items[currentIndex].gameObject;
        currentItem.GetComponent<TematicaItem>().SelectForever(_leaderboard);

        UpdateDots();

        if (_leaderboard)
        {
            // if (manager == null)
            {
                manager = FindObjectOfType<ManagerRankingForever>();
                manager.OnEnable(); // acá tenés el objeto que lo contiene
            }
        }
    }
}