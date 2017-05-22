//NOTE: This handler is strange b/c it lives and works on the server
public class ColorToggleInteractHandler : ClickInteractionHandler
{
    private const float RANGE = 20f;
    public ColorToggle ColorToggle;

    public override void OnInteract(PlayerInteractionController controller, bool click = false)
    {
        if ((transform.position - controller.transform.position).sqrMagnitude > RANGE * RANGE)
            return;

        ColorToggle.Toggle();
    }

    protected override void SetActiveState(bool state)
    {
    }
}