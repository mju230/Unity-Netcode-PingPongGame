using UnityEngine;
using UnityEngine.EventSystems;

public class MatchingPanelController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform panelRect;
    public bool isHovered;
    public float speed = 10f;


    public void Update()
    {
        float targetY = isHovered ? 20 : 0;
        Vector2 targetPos = new Vector2(panelRect.anchoredPosition.x, targetY);
        panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, targetPos, Time.deltaTime * speed);
    }

    void OnEnable()
    {
        isHovered = false;
        if (panelRect != null)
            panelRect.anchoredPosition = new Vector2(0,0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}
