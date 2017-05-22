using System;
using System.Collections.Generic;
using UnityEngine;

public enum SculptureType
{
    TransparentCubes,
    AvoidCubes,
    ShrinkCubes,
    PixelBoard,
    TextureScrollWall,
    TextureZoomScrollWall,
    PointingSwarm,
    Seating
}

public class SculptureInteractionHandler : InteractionHandler
{
    [SerializeField]
    protected Sculpture Manager;

    public ReticuleType ReticuleType;
    public SculptureType Type;
    public bool SuperUserOnly = false;

    private void Start()
    {
        if (Manager == null)
            Debug.LogError("Manager is null on SculptureInteractionHandler", this);
    }

    public override void OnInteract(PlayerInteractionController controller, bool click = false)
    {
        if (click)
            return;

        HUDManager.SculptureOptions.SetHandler(this);
    }

    public virtual Dictionary<string, Func<string>> GetData()
    {
        return Manager.GetCurrentData();
    }

    public virtual string GetIdentifier()
    {
        return Manager.netId.ToString();
    }

    protected override void SetActiveState(bool state)
    {
        if (state)
        {
            if (ReticuleType != ReticuleType.None)
                HUDManager.ShowReticule(ReticuleType, gameObject);
            if (!SuperUserOnly || HUDManager.Singleton.HasSuperUserAccess)
                HUDManager.ShowInteractionText(gameObject);
        }
        else
        {
            if (ReticuleType != ReticuleType.None)
                HUDManager.HideReticule();
            if (!SuperUserOnly || HUDManager.Singleton.HasSuperUserAccess)
                HUDManager.HideInteractionText();
        }
    }
}