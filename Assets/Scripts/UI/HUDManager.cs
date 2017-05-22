using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class HUDManager : NetworkBehaviour
{
    public static HUDManager Singleton;

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

    private CommandProcessor _processor;
    private CommandProcessor m_Processor
    {
        get
        {
            if (_processor != null) return _processor;

            var processor = m_Player.GetComponent<CommandProcessor>();

            _processor = processor;
            _processor.OnOutputReceived = PostCommandMessage;
            return _processor;
        }
    }

    private NetworkedPlayerHUDBridge _networkBridge;
    private NetworkedPlayerHUDBridge m_NetworkBridge
    {
        get
        {
            if (_networkBridge != null) return _networkBridge;

            var bridge = m_Player.GetComponent<NetworkedPlayerHUDBridge>();

            _networkBridge = bridge;
            return _networkBridge;
        }
    }

    private readonly SyncListString m_ConnectedPlayerNames = new SyncListString();
    private readonly List<GameObject> m_ConnectedPlayers = new List<GameObject>();

    private bool m_ShownInteractWithContext;
    private GameObject m_InteractionContext;
    public GameObject InteractionText;

    private bool m_ShownReticuleWithContext;
    private GameObject m_ReticuleContext;
    public ReticuleController Reticule;

    private bool m_HasSuperUserAccess;
    public bool HasSuperUserAccess
    {
        get { return m_HasSuperUserAccess; }
        set
        {
            m_HasSuperUserAccess = value;
            SuperUserGroup.SetActive(m_HasSuperUserAccess);
        }
    }
    public readonly string SuperUserPassword = "RomeShow2016";
    public GameObject SuperUserGroup;
    public GameObject PasswordEntryGroup;
    public GameObject DirectorPanel;
    public GameObject ControlPanel;
    public MessageBoxController MessageBox;
    public SculptureOptionsController SculptureOptions;
    public GameObject BeaconDialog;
    public Dropdown BeaconOptionsDropDown;
    public SelectionPanel PlayersSelection;
    public Action<string> ConveyanceSitAction;
    public Action ConveyanceCancelAction;
    public Color DirectorMessageColor;
    public Color ServerMessageColor;
    public Color UserMessageColor;
    public InputField TextInputField;
    public InputField DirectorInputField;
    public Text InfoText;
    public AudioSource StreamingAudioSource;
    public Toggle PredationToggle;
    public Canvas MainCanvas;

    private void Awake()
    {
        if (Singleton == null)
            Singleton = this;
    }

    private void Start()
    {
        Player_ID.OnPlayerSetupComplete += ServerAddPlayer;

        PlayersSelection.ClearOptions();
        PlayersSelection.AddOptions(m_ConnectedPlayerNames.ToArray());

        SuperUserGroup.SetActive(false);

        if (isServer)
        {
            ((ArsNetworkManager) NetworkManager.singleton).OnPlayerDisconnect += ServerRemovePlayer;
        }

        if (isServer && !isClient)
            return;

        m_ConnectedPlayerNames.Callback = OnConnectedPlayersChanged;
    }

    private void OnConnectedPlayersChanged(SyncList<string>.Operation op, int itemindex)
    {
        PlayersSelection.ClearOptions();
        PlayersSelection.AddOptions(m_ConnectedPlayerNames.ToArray());
    }

    private void Update()
    {
        if (isServer)
        {
            if (isClient && !HasSuperUserAccess)
                HasSuperUserAccess = true;

            //Disconnected player objects will be null
            if (m_ConnectedPlayers.Any(o => o == null))
            {
                m_ConnectedPlayers.RemoveAll(o => o == null);
                m_ConnectedPlayerNames.Clear();
                foreach (var player in m_ConnectedPlayers)
                {
                    m_ConnectedPlayerNames.Add(player.name);
                }
            }
            if (!isClient)
                return;
        }

        if (m_ShownInteractWithContext && m_InteractionContext == null)
            HideInteractionText();

        if (m_ShownReticuleWithContext && m_ReticuleContext == null)
            HideReticule();

        if (StreamingAudioSource.clip != null 
            && !StreamingAudioSource.isPlaying 
            && StreamingAudioSource.clip.loadState == AudioDataLoadState.Loaded)
        {
            StreamingAudioSource.Play();
        }

        if (TextInputField.isFocused || DirectorInputField.isFocused)
        {
            DisablePlayerInput();
        }

        if (CrossPlatformInputManager.GetButtonDown("HUDToggle"))
        {
            MainCanvas.enabled = !MainCanvas.enabled;
        }

        if (!HasSuperUserAccess && CrossPlatformInputManager.GetButtonDown("SuperUserToggle"))
        {
            PasswordEntryGroup.SetActive(!PasswordEntryGroup.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Return) && GUI.GetNameOfFocusedControl() == string.Empty)
        {
            TextInputField.ActivateInputField();
            DisablePlayerInput();
        }
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            EnablePlayerInput();
        }
    }

    private bool IsPointerOverUIObject()
    {
        var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void OnPasswordEntered(string pw)
    {
        if (HasSuperUserAccess)
            return;

        if (string.Equals(pw, SuperUserPassword, StringComparison.CurrentCultureIgnoreCase))
        {
            HasSuperUserAccess = true;
            PasswordEntryGroup.SetActive(false);
        }
    }

    public void ShowBeaconDialog(List<string> beaconNames)
    {
        BeaconOptionsDropDown.ClearOptions();
        BeaconOptionsDropDown.AddOptions(beaconNames);
        BeaconOptionsDropDown.value = 0;

        BeaconDialog.SetActive(true);
    }

    public void HideBeaconDialog()
    {
        BeaconDialog.SetActive(false);
        ConveyanceSitAction = null;
        ConveyanceCancelAction = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextObj">
    /// Optionally, a GameObject to watch for the deletion of. 
    /// When the object becomes null, The interaction text will be automatically hidden.
    /// </param>
    public void ShowInteractionText(GameObject contextObj = null)
    {
        m_ShownInteractWithContext = contextObj != null;
        m_InteractionContext = contextObj;

        InteractionText.SetActive(true);
    }

    public void HideInteractionText()
    {
        InteractionText.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextObj">
    /// Optionally, a GameObject to watch for the deletion of. 
    /// When the object becomes null, The reticule will be automatically hidden.
    /// </param>
    public void ShowReticule(ReticuleType type, GameObject contextObj = null)
    {
        m_ShownReticuleWithContext = contextObj != null;
        m_ReticuleContext = contextObj;

        Reticule.CurrentReticule = type;
    }

    public void HideReticule()
    {
        Reticule.CurrentReticule = ReticuleType.None;
    }

    public void PostMessage(string msg)
    {
        TextInputField.text = string.Empty;

        if (string.IsNullOrEmpty(msg))
            return;

        if (msg.StartsWith("/"))
        {
            ForwardCommand(msg);
            return;
        }

        m_NetworkBridge.PostMessage(m_Player.name + ": " + msg);

        TextInputField.ActivateInputField();
    }

    public void ForwardCommand(string cmd)
    {
        m_Processor.ProcessCommand(cmd.Substring(1));
    }

    public void ForwardDataCommand(Data key, string value)
    {
        ForwardCommand("/data " + key.FastToString() + " " + value);
    }

    public void PostDirectorMessage(string msg)
    {
        DirectorInputField.text = string.Empty;

        if (string.IsNullOrEmpty(msg))
            return;

        m_NetworkBridge.PostDirectorMessage(msg);

        DirectorInputField.ActivateInputField();
    }

    [Server]
    public void ServerPostDirectorMessage(string msg)
    {
        RpcPostDirectorMessage(msg);
    }

    [ClientRpc]
    private void RpcPostDirectorMessage(string msg)
    {
        MessageBox.PostMessage("Director: " + msg, DirectorMessageColor);
    }

    [Server]
    public void ServerPostMessage(string msg)
    {
        RpcPostMessage(msg);
    }

    [ClientRpc]
    private void RpcPostMessage(string msg)
    {
        MessageBox.PostMessage(msg, UserMessageColor);
    }

    public void PostCommandMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return;
        MessageBox.PostMessage("Server: " + msg, ServerMessageColor);
    }

    [Server]
    public void ServerAddPlayer(GameObject obj)
    {
        if (m_ConnectedPlayers.Any(
                go => go.GetComponent<NetworkIdentity>().netId == obj.GetComponent<NetworkIdentity>().netId))
            return;

        m_ConnectedPlayerNames.Add(obj.name);
        m_ConnectedPlayers.Add(obj);
    }

    [Server]
    public void ServerRemovePlayer(GameObject obj)
    {
        m_ConnectedPlayerNames.Remove(obj.name);
    }

    public void OnConveyanceSit()
    {
        if (ConveyanceSitAction != null)
        {
            ConveyanceSitAction(BeaconOptionsDropDown.options[BeaconOptionsDropDown.value].text);
        }
    }

    public void OnConveyanceCancel()
    {
        if (ConveyanceCancelAction != null)
            ConveyanceCancelAction();
    }

    private void DisablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().DeactivateGameInput();
    }

    private void EnablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().EnableGameInput();
    }

    public void OnEjectPlayerClick()
    {
        if (!HasSuperUserAccess)
            return;

        foreach (var selectedOption in PlayersSelection.SelectedOptions())
        {
            m_NetworkBridge.KickPlayer(selectedOption);
        }
    }

    public void OnTeleportPlayerClick()
    {
        if (!HasSuperUserAccess)
            return;

        foreach (var selectedOption in PlayersSelection.SelectedOptions())
        {
            m_NetworkBridge.TeleportPlayer(selectedOption, m_Player.transform.position + (m_Player.transform.forward * 3f));
        }
    }

    public void SetInfoText(string info)
    {
        InfoText.text = "Info: " + info;
    }

    public void SetVolume(float val)
    {
        AudioListener.volume = val;
    }

    [Client]
    public void BroadcastAudioStreamToPlay(string streamUrl)
    {
        if (!HasSuperUserAccess)
            return;

        m_NetworkBridge.PlayAudioStream(PlayersSelection.SelectedOptions().ToArray(), streamUrl);
    }

    [Client]
    public void PlayAudioStream(string streamUrl)
    {
        var www = new WWW(streamUrl);
        StreamingAudioSource.clip = www.GetAudioClip(false, true);
    }
}
