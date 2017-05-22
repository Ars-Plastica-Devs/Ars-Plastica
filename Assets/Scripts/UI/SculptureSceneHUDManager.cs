using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class SculptureSceneHUDManager : NetworkBehaviour
{
    public SculptureSceneHUDManager Singleton;

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
    public MessageBoxController MessageBox;
    public SculptureOptionsController SculptureOptions;
    public SelectionPanel PlayersSelection;
    public GameObject SuperUserGroup;
    public GameObject PasswordEntryGroup;
    public InputField TextInputField;
    public InputField DirectorInputField;
    public Text InfoText;
    public Color DirectorMessageColor;
    public Color ServerMessageColor;
    public Color UserMessageColor;
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
            ((ArsNetworkManager)NetworkManager.singleton).OnPlayerDisconnect += ServerRemovePlayer;
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

    [Server]
    public void ServerAddPlayer(GameObject obj)
    {
        m_ConnectedPlayerNames.Add(obj.name);
        m_ConnectedPlayers.Add(obj);
    }

    [Server]
    public void ServerRemovePlayer(GameObject obj)
    {
        m_ConnectedPlayerNames.Remove(obj.name);
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

    public void SetInfoText(string info)
    {
        InfoText.text = "Info: " + info;
    }

    public void SetVolume(float val)
    {
        AudioListener.volume = val;
    }

    private void EnablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().EnableGameInput();
    }

    private void DisablePlayerInput()
    {
        m_Player.GetComponent<PlayerInteractionController>().DeactivateGameInput();
    }
}
