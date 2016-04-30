public class ConveyanceInteractionHandler : InteractionHandler
{
    public override void OnInteract(PlayerInteractionController controller)
    {
        controller.DetachFromConveyance();
    }

    protected override void SetActiveState(bool state)
    {
        
    }
}
