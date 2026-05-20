using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SnapScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración de UI")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public Button nextBtn;
    public Button prevBtn;

    [Header("Ajustes de Snap")]
    public float snapSpeed = 10f;

    [Header("Swipe")]
    public float swipePixels = 120f;

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
    private bool swipeConsumed = false;

    private Vector2 dragStartPosition;

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

            // Agregamos automáticamente el forwarder al prefab/item
            SnapScrollDragForwarder forwarder = child.GetComponent<SnapScrollDragForwarder>();

            if (forwarder == null)
            {
                forwarder = child.gameObject.AddComponent<SnapScrollDragForwarder>();
            }

            forwarder.snapScrollRect = this;
        }

        if (items.Count == 0)
            return;

        Adjust();

        // 2. Calcular las posiciones normalizadas (0 a 1) para cada item
        itemPositions = new float[items.Count];

        if (items.Count == 1)
        {
            itemPositions[0] = 0f;
            currentIndex = 0;
        }
        else
        {
            float distance = 1f / (items.Count - 1);

            for (int i = 0; i < items.Count; i++)
            {
                itemPositions[i] = distance * i;
            }
        }

        CreateDots();

        // 3. Configurar botones
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(() => Next());
        }

        if (prevBtn != null)
        {
            prevBtn.onClick.RemoveAllListeners();
            prevBtn.onClick.AddListener(() => Previous());
        }

        enabled = true;

        if (scrollRect != null)
        {
            scrollRect.velocity = Vector2.zero;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, items.Count - 1);
        currentItem = items[currentIndex].gameObject;

        UpdateBannerFade();
        UpdateDots();
    }

    private void OnDisable()
    {
        if (nextBtn != null)
            nextBtn.onClick.RemoveAllListeners();

        if (prevBtn != null)
            prevBtn.onClick.RemoveAllListeners();
    }

    public void ClearChildren(Transform panel)
    {
        if (panel == null)
            return;

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
        if (content == null || content.childCount == 0)
            return;

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
            totalWidth += (layout.spacing / 2) * nItems;
            totalWidth += layout.padding.left + layout.padding.right;
        }

        // 5. Aplicamos el tamaño final
        content.sizeDelta = new Vector2(totalWidth, content.sizeDelta.y);
    }

    void Update()
    {
        if (scrollRect == null || itemPositions == null || itemPositions.Length == 0)
            return;

        if (!isDragging)
        {
            // Interpolar suavemente hacia la posición del item actual
            float targetPos = itemPositions[currentIndex];

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                targetPos,
                Time.deltaTime * snapSpeed
            );
        }

        // Fade de banners según cercanía al centro
        UpdateBannerFade();
    }

    private void UpdateBannerFade()
    {
        if (items == null || items.Count == 0 || itemPositions == null || scrollRect == null)
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginSwipe(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DragSwipe(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndSwipe();
    }

    public void BeginSwipe(Vector2 pointerPosition)
    {
        if (items == null || items.Count == 0)
            return;

        isDragging = true;
        swipeConsumed = false;

        dragStartPosition = pointerPosition;

        if (scrollRect != null)
            scrollRect.velocity = Vector2.zero;
    }

    public void DragSwipe(Vector2 pointerPosition)
    {
        if (!isDragging)
            return;

        if (swipeConsumed)
            return;

        if (items == null || items.Count == 0)
            return;

        float deltaX = pointerPosition.x - dragStartPosition.x;

        if (Mathf.Abs(deltaX) < swipePixels)
            return;

        swipeConsumed = true;
        isDragging = false;

        if (scrollRect != null)
            scrollRect.velocity = Vector2.zero;

        if (deltaX < 0f)
        {
            // Swipe de derecha a izquierda = igual que presionar botón Next
            if (nextBtn != null)
                nextBtn.onClick.Invoke();
            else
                Next();
        }
        else
        {
            // Swipe de izquierda a derecha = igual que presionar botón Previous
            if (prevBtn != null)
                prevBtn.onClick.Invoke();
            else
                Previous();
        }

        if (scrollRect != null)
            scrollRect.velocity = Vector2.zero;
    }

    public void EndSwipe()
    {
        isDragging = false;
        swipeConsumed = false;

        if (scrollRect != null)
            scrollRect.velocity = Vector2.zero;

        UpdateDots();
    }

    public void Next(bool _leaderboard = false)
    {
        if (items == null || items.Count == 0)
            return;

        currentIndex++;

        if (currentIndex > items.Count - 1)
            currentIndex = 0;

        UpdateItem(_leaderboard);
    }

    public void Previous(bool _leaderboard = false)
    {
        if (items == null || items.Count == 0)
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = items.Count - 1;

        UpdateItem(_leaderboard);
    }

    public void UpdateItem(bool _leaderboard)
    {
        if (items == null || items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, items.Count - 1);

        // Actualizar el GameObject actual para referencia externa
        currentItem = items[currentIndex].gameObject;

        TematicaItem tematicaItem = currentItem.GetComponent<TematicaItem>();

        if (tematicaItem != null)
        {
            tematicaItem.SelectForever(_leaderboard);
        }

        UpdateDots();

        if (scrollRect != null)
            scrollRect.velocity = Vector2.zero;

        if (_leaderboard)
        {
            manager = FindObjectOfType<ManagerRankingForever>();

            if (manager != null)
            {
                manager.OnEnable();
            }
        }
    }
}