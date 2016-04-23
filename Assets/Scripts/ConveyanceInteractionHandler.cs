public class ConveyanceInteractionHandler : InteractionHandler
{
    private enum InteractState 
    {
        None,
        ShowingText,
        DialogOptions,
        HoldingPlayer
    }

    private PlayerInteractionController m_PlayerController;
    private InteractState m_State = InteractState.None;

    public override void OnInteract(PlayerInteractionController controller)
    {
        m_PlayerController = controller;

        if (m_State == InteractState.ShowingText)
        {
            HUDManager.HideInteractionText();
            HUDManager.ShowConveyanceDialog();
            HUDManager.ConveyanceSitAction = OnSit;
            HUDManager.ConveyanceCancelAction = OnSitCancel;

            m_State = InteractState.DialogOptions;
            controller.DeactivateGameInput();
        }
        else if (m_State == InteractState.HoldingPlayer)
        {
            m_State = InteractState.None;
            controller.DetachFromConveyance();
        }
    }

    protected override void SetActive(bool state)
    {
        if (state)
        {
            HUDManager.ShowInteractionText();
            HUDManager.HideConveyanceDialog();
            m_State = InteractState.ShowingText;
        }
        else
        {
            HUDManager.HideInteractionText();
            HUDManager.HideConveyanceDialog();
            m_State = InteractState.None;
        }
    }

    private void OnSit()
    {
        HUDManager.HideConveyanceDialog();
        m_State = InteractState.HoldingPlayer;
        m_PlayerController.SitOnConveyance(gameObject);
    }

    private void OnSitCancel()
    {
        HUDManager.HideConveyanceDialog();
        m_PlayerController.EnableGameInput();
        m_State = InteractState.None;
    }
}
