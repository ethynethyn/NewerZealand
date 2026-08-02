using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEnableObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    public GameObject targetObject;

    void Start()
    {
        if (targetObject != null)
            targetObject.SetActive(false); // make sure it's off at start
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }
}