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
    private BeaconController m_Controller;

    private void Start()
    {
        m_Controller = GetComponent<BeaconController>();
    }

    public override void OnInteract(PlayerInteractionController controller)
    {
        m_PlayerController = controller;

        if (m_State == InteractState.ShowingText)
        {
            HUDManager.HideInteractionText();
            var pathNames = m_Controller.GetPaths();
            HUDManager.ShowBeaconDialog(pathNames);
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

    private void OnSit(string pathName)
    {
        var reverse = m_Controller.GetPaths(true).Find(s => s.StartsWith(pathName)).ToLower().EndsWith("end");
        m_PlayerController.RequestSeatOnConveyance(pathName, reverse);
        Active = false;
    }

    private void OnSitCancel()
    {
        HUDManager.HideBeaconDialog();
        m_PlayerController.EnableGameInput();
        m_State = InteractState.None;
    }
}
