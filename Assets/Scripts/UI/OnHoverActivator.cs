using UnityEngine;
using UnityEngine.EventSystems;

public class OnHoverActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject Target;

    private void Start()
    {
        if (Target == null)
            Debug.LogError("Target is null in OnHoverActivator", this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Target.SetActive(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        Target.SetActive(false);
    }
}
