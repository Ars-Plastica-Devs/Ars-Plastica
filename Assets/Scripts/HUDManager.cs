using System;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public GameObject ConveyanceDialog;
    public GameObject InteractionText;
    public Action ConveyanceSitAction;
    public Action ConveyanceCancelAction;

    public void ShowConveyanceDialog()
    {
        ConveyanceDialog.SetActive(true);
    }

    public void HideConveyanceDialog()
    {
        ConveyanceDialog.SetActive(false);
        ConveyanceSitAction = null;
        ConveyanceCancelAction = null;
    }

    public void ShowInteractionText()
    {
        InteractionText.SetActive(true);
    }

    public void HideInteractionText()
    {
        InteractionText.SetActive(false);
    }

    public void OnConveyanceSit()
    {
        if (ConveyanceSitAction != null)
            ConveyanceSitAction();
    }

    public void OnConveyanceCancel()
    {
        if (ConveyanceCancelAction != null)
            ConveyanceCancelAction();
    }
}
