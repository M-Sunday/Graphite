using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image), typeof(RectTransform))]
public class SlotSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.2f;
    public float scaleTransitionSpeed = 8f;
    public float colorTransitionSpeed = 10f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    private RectTransform rectTransform;
    private Image image;
    private Vector3 originalScale;
    private bool isHovering = false;

    // Static reference to the currently hovered slot
    private static SlotSelect currentlyHovered;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalScale = rectTransform.localScale;

        // Initialize with normal color
        image.color = normalColor;
    }

    private void Update()
    {
        // Smooth scale transition
        Vector3 targetScale = isHovering ? originalScale * hoverScaleMultiplier : originalScale;
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleTransitionSpeed
        );

        // Smooth color transition
        Color targetColor = isHovering ? hoverColor : normalColor;
        image.color = Color.Lerp(image.color, targetColor, Time.unscaledDeltaTime * colorTransitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Unhover the previous slot
        if (currentlyHovered != null && currentlyHovered != this)
        {
            currentlyHovered.SetHover(false);
        }
        currentlyHovered = this;
        SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentlyHovered == this)
        {
            SetHover(false);
            currentlyHovered = null;
        }
    }

    private void SetHover(bool hover)
    {
        isHovering = hover;
        // Color and scale are handled smoothly in Update()
    }

    private void OnDisable()
    {
        if (currentlyHovered == this)
            currentlyHovered = null;
        rectTransform.localScale = originalScale;
        image.color = normalColor;
        isHovering = false;
    }
}
