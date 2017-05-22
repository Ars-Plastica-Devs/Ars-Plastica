using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoScroll : MonoBehaviour
{
    private Canvas m_Canvas;
    private float m_LastHeight;
    public RectTransform VisibleSpace;
    public RectTransform Content;
    public ScrollRect Scroller;
    public float Speed;
    public float ScrollDelay = 1f;

    private void Start()
    {
        m_Canvas = GetComponentInParent<Canvas>();
        m_LastHeight = Content.sizeDelta.y;
    }

    private void Update()
    {
        if (m_LastHeight != Content.sizeDelta.y)
        {
            Scroller.verticalNormalizedPosition = 1f;
            m_LastHeight = Content.sizeDelta.y;

            if (VisibleSpace.rect.height < Content.sizeDelta.y)
            {
                StopAllCoroutines();
                StartCoroutine(DoAutoScroll());
            }
        }
    }

    private IEnumerator DoAutoScroll()
    {
        yield return new WaitForSeconds(ScrollDelay);
        var contentRect = GetScreenRect(Content, m_Canvas);
        var visibleRect = GetScreenRect(VisibleSpace, m_Canvas);

        while (contentRect.yMax > visibleRect.yMax && Scroller.verticalNormalizedPosition > 0f)
        {
            contentRect = GetScreenRect(Content, m_Canvas);
            visibleRect = GetScreenRect(VisibleSpace, m_Canvas);

            Scroller.verticalNormalizedPosition -= (Speed * Time.deltaTime);
            yield return null;
        }
    }

    public static Rect GetScreenRect(RectTransform rectTransform, Canvas canvas)
    {

        var corners = new Vector3[4];
        var screenCorners = new Vector3[2];

        rectTransform.GetWorldCorners(corners);

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
        {
            screenCorners[0] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[1]);
            screenCorners[1] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[3]);
        }
        else
        {
            screenCorners[0] = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            screenCorners[1] = RectTransformUtility.WorldToScreenPoint(null, corners[3]);
        }

        screenCorners[0].y = Screen.height - screenCorners[0].y;
        screenCorners[1].y = Screen.height - screenCorners[1].y;

        return new Rect(screenCorners[0], screenCorners[1] - screenCorners[0]);
    }
}