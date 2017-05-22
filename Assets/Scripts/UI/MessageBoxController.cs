using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageBoxController : MonoBehaviour
{
    public enum GrowDirection
    {
        Up,
        Down
    }

    private readonly Queue<GameObject> m_Messages = new Queue<GameObject>(); 

    public GrowDirection Grows = GrowDirection.Up;
    public GameObject MessagePrefab;
    public float HorizontalPad = 2f;
    public GameObject MessageParentingTarget;

    private void Start()
    {
        if (MessagePrefab == null)
            Debug.LogError("MessagePrefab is null in MessageBoxController", this);
        if (MessageParentingTarget == null)
            Debug.LogError("MessageParentingTarget is null in MessageBoxController", this);
    }

    public void PostMessage(string msg, Color color)
    {
        var message = Instantiate(MessagePrefab);
        message.GetComponent<Text>().text = msg;
        message.GetComponent<Text>().color = color;
        message.GetComponent<Text>().SetAllDirty();
        message.transform.SetParent(MessageParentingTarget.transform, false);

        var rectTrans = GetComponent<RectTransform>();
        var bottomLeft = new Vector3(transform.position.x - (rectTrans.rect.width / 2f), transform.position.y - (rectTrans.rect.height / 2f), transform.position.z);
        var pad = new Vector3(HorizontalPad, 0, 0);

        //Force messages to be laid out properly now instead of at end of frame
        Canvas.ForceUpdateCanvases();
        var offsetAmount = new Vector3(0, message.GetComponent<RectTransform>().rect.height, 0);
        foreach (var o in m_Messages)
        {
            o.transform.position += offsetAmount;
        }

        m_Messages.Enqueue(message);

        if (Grows == GrowDirection.Up)
        {
            message.transform.position = bottomLeft + pad;
        }

        if (m_Messages.Count > 20)
        {
            Destroy(m_Messages.Dequeue());
        }
    }
}
