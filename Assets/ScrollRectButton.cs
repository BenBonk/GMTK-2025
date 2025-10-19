using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ScrollRectButton : MonoBehaviour, IBeginDragHandler,  IDragHandler, IEndDragHandler, IScrollHandler
{
    // Private variables
    //--------------------------------

    public ScrollRect m_TargetScrollRect;
   
    // Public methods
    //--------------------------------
   
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_TargetScrollRect.OnBeginDrag(eventData);
    }
   
    public void OnDrag(PointerEventData eventData)
    {
        m_TargetScrollRect.OnDrag(eventData);
    }
   
    public void OnEndDrag(PointerEventData eventData)
    {
        m_TargetScrollRect.OnEndDrag(eventData);
    }
   
    public void OnScroll(PointerEventData data)
    {
        m_TargetScrollRect.OnScroll(data);
    }

    // Private methods
    //--------------------------------

    void Start()
    {
        m_TargetScrollRect = FindComponentInParent<ScrollRect>(transform);
    }
    public static T FindComponentInParent<T>(Transform transform)
    {
        while (transform.parent != null)
        {
            T component = transform.parent.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
            transform = transform.parent;
        }
        return default(T);
    }
}