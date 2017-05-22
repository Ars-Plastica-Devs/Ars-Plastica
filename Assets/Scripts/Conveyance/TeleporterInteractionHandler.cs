using UnityEngine;

public class TeleporterInteractionHandler : ClickInteractionHandler
{
    public ReticuleType ReticuleType = ReticuleType.Hand;
    public Transform TeleportTarget;
    public Vector3 TeleportOffset = Vector3.zero;

    private void Start()
    {
        ServerSide = false;
        
        if (TeleportTarget == null)
            Debug.LogError("TeleportTarget is null in TeleportInteractionHandler", this);
    }

    public override void OnInteract(PlayerInteractionController controller, bool clickInteract = false)
    {
        if (!clickInteract)
            return;

        var targetPos = TeleportTarget.position + TeleportOffset;
        HUDManager.ForwardCommand("/set-loc " + targetPos.x + " " + targetPos.y + " " + targetPos.z);
    }

    protected override void SetActiveState(bool state)
    {
        if (state)
        {
            if (ReticuleType != ReticuleType.None)
                HUDManager.ShowReticule(ReticuleType, gameObject);
        }
        else
        {
            if (ReticuleType != ReticuleType.None)
                HUDManager.HideReticule();
        }
    }
}
