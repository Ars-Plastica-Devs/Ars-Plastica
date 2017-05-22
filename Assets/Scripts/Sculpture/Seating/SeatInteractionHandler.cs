public class SeatInteractionHandler : ClickInteractionHandler
{
    public override void OnInteract(PlayerInteractionController controller, bool clickInteract = false)
    {
        
    }

    protected override void SetActiveState(bool state)
    {
        if (state)
        {
            HUDManager.ShowReticule(ReticuleType.Crosshair, gameObject);
        }
        else
        {
            HUDManager.HideReticule();
        }
    }
}
