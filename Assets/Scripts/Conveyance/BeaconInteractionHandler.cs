public class BeaconInteractionHandler : InteractionHandler
{
    private enum InteractState 
    {
        None,
        ShowingText,
        DialogOptions
    }

    private PlayerInteractionController m_PlayerController;
    private InteractState m_State = InteractState.None;
    public BeaconController Controller;

    public override void OnInteract(PlayerInteractionController controller, bool click = false)
    {
        m_PlayerController = controller;

        if (m_State == InteractState.ShowingText)
        {
            HUDManager.HideInteractionText();
            var beaconNames = Controller.GetBeacons();
            HUDManager.ShowBeaconDialog(beaconNames);
            HUDManager.ConveyanceSitAction = OnSit;
            HUDManager.ConveyanceCancelAction = OnSitCancel;

            m_State = InteractState.DialogOptions;
            controller.DeactivateGameInput();
        }
    }

    protected override void SetActiveState(bool state)
    {
        if (state)
        {
            HUDManager.ShowInteractionText();
            HUDManager.HideBeaconDialog();
            m_State = InteractState.ShowingText;
        }
        else
        {
            HUDManager.HideInteractionText();
            HUDManager.HideBeaconDialog();
            m_State = InteractState.None;
        }
    }

    private void OnSit(string otherBeaconName)
    {
        m_PlayerController.RequestSeatOnConveyance(Controller.Name, otherBeaconName);
        Active = false;
    }

    private void OnSitCancel()
    {
        HUDManager.HideBeaconDialog();
        m_PlayerController.EnableGameInput();
        m_State = InteractState.None;
    }
}
