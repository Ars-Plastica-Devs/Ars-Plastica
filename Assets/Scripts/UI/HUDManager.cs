using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public GameObject BeaconDialog;
    public GameObject InteractionText;
    public Dropdown PathOptionsDropDown;
    public Action<string> ConveyanceSitAction;
    public Action ConveyanceCancelAction;

    public void ShowBeaconDialog(List<string> pathNames)
    {
        PathOptionsDropDown.ClearOptions();
        PathOptionsDropDown.AddOptions(pathNames);

        BeaconDialog.SetActive(true);
    }

    public void HideBeaconDialog()
    {
        BeaconDialog.SetActive(false);
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
        {
            ConveyanceSitAction(PathOptionsDropDown.options[PathOptionsDropDown.value].text);
        }
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
