using System;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    private GameObject _player;
    private GameObject m_Player
    {
        get
        {
            if (_player != null) return _player;

            _player = GameObject.FindGameObjectWithTag("Player");
            return _player;
        }
    }

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

    public void DisablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().DeactivateGameInput();
    }

    public void EnablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().EnableGameInput();
    }
}
