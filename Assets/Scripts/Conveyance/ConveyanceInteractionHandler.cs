public class ConveyanceInteractionHandler : InteractionHandler
{
    public override void OnInteract(PlayerInteractionController controller, bool click = false)
    {
        controller.DetachFromConveyance();
    }

    protected override void SetActiveState(bool state)
    {
        
    }
}
