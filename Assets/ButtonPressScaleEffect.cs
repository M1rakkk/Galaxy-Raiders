using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] float pressedScale = 0.95f;

    Vector3 normalScale;

    void Awake()
    {
        normalScale = transform.localScale;
    }

    void OnEnable()
    {
        if (normalScale == Vector3.zero)
        {
            normalScale = transform.localScale;
        }

        transform.localScale = normalScale;
    }

    void OnDisable()
    {
        transform.localScale = normalScale == Vector3.zero ? Vector3.one : normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = normalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = normalScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = normalScale;
    }
}
