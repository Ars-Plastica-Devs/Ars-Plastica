using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerInteractionController : NetworkBehaviour
{
    private InteractionHandler m_InteractHandler;
    private FirstPersonController m_Controller;
    private GameObject m_ConveyanceAttachedTo;

    private int m_CheckCounter;
    private const int CHECK_RATE = 4;

    public float InteractDistance = 5f;
    public bool InteractInputEnabled = true;
    public bool CheckForInteractions = true;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }
        m_Controller = GetComponent<FirstPersonController>();
    }

    public void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyUp(KeyCode.F) && InteractInputEnabled)
        {
            OnInteract();
        }

        m_CheckCounter++;

        if (m_CheckCounter > CHECK_RATE && CheckForInteractions)
        {
            m_CheckCounter = 0;

            DoInteractableCheck();
        }
    }

    private void OnInteract()
    {
        if (m_InteractHandler == null) return;
        m_InteractHandler.OnInteract(this);
    }

    private void DoInteractableCheck()
    {
        var rayHits = Physics.SphereCastAll(m_Controller.GetCameraLocation(), 3f, m_Controller.GetLookDirection(), InteractDistance);
        rayHits = rayHits.Where(rh => rh.transform.gameObject.GetComponent<InteractionHandler>() != null).ToArray();
        //Debug.DrawLine(m_Controller.GetCameraLocation(), m_Controller.GetCameraLocation() + (m_Controller.GetLookDirection() * InteractDistance), Color.red);
        if (rayHits.GetLength(0) == 0)
        {
            if (m_InteractHandler != null)
            {
                m_InteractHandler.Active = false;
                m_InteractHandler = null;
            }
            InteractInputEnabled = false;
            return;
        }

        var closest = GetClosestRayHit(rayHits);
        var interactable = closest.transform.gameObject.GetComponent<InteractionHandler>();

        if (interactable == null)
        {
            if (m_InteractHandler != null)
            {
                m_InteractHandler.Active = false;
                m_InteractHandler = null;
            }
            InteractInputEnabled = false;
        }
        else
        {
            InteractInputEnabled = true;
            m_InteractHandler = interactable;
            m_InteractHandler.Active = true;
        }
    }
    
    public void DeactivateGameInput()
    {
        if (!isLocalPlayer) return;
        m_Controller.enabled = false;
        m_Controller.MouseOnly = false;
    }

    public void DeactivateGameInputExceptMouse()
    {
        if (!isLocalPlayer) return;
        m_Controller.enabled = true;
        m_Controller.MouseOnly = true;
    }

    public void EnableGameInput()
    {
        if (!isLocalPlayer) return;
        m_Controller.enabled = true;
        m_Controller.MouseOnly = false;
    }

    public void RequestSeatOnConveyance(string pathName, bool reverse = false)
    {
        CmdRequestSeatOnConveyance(pathName, reverse);

        //Deactivate these right away so we arent waiting for response from server
        CheckForInteractions = false;
        DeactivateGameInputExceptMouse();
    }

    [ClientRpc]
    private void RpcSitOnConveyance(GameObject con)
    {
        if (!isLocalPlayer) return;

        m_ConveyanceAttachedTo = con;

        m_InteractHandler = m_ConveyanceAttachedTo.GetComponent<InteractionHandler>();
    }

    [Command]
    private void CmdRequestSeatOnConveyance(string pathName, bool reverse)
    {
        //Spawn the conveyance, then call our RPC
        var pathManager = GameObject.FindGameObjectWithTag("PathManager").GetComponent<PathManager>();
        var obj = pathManager.SpawnConveyance(pathName, reverse);
        var con = obj.GetComponent<ConveyanceController>();

        pathManager.NotifyOfAttachToConveyance(this, con);
        RpcSitOnConveyance(obj);
    }

    public void DetachFromConveyance()
    {
        m_InteractHandler.Active = false;
        m_InteractHandler = null;
        //m_ConveyanceAttachedTo.GetComponent<ConveyanceController>().DetachObject(gameObject);
        EnableGameInput();
        CheckForInteractions = true;

        CmdDetachFromConveyance(m_ConveyanceAttachedTo);

        m_ConveyanceAttachedTo = null;
        DoInteractableCheck();
    }

    [ClientRpc]
    private void RpcDetachFromConveyance()
    {
        DetachFromConveyance();
    }

    [Server]
    public void DetachClientsFromConveyance()
    {
        RpcDetachFromConveyance();
    }

    [Command]
    private void CmdDetachFromConveyance(GameObject obj)
    {
        var pathManager = GameObject.FindGameObjectWithTag("PathManager").GetComponent<PathManager>();
        pathManager.RemovePlayerFromConveyance(this, obj.GetComponent<ConveyanceController>());
        m_ConveyanceAttachedTo = null;
    }

    private static RaycastHit GetClosestRayHit(IEnumerable<RaycastHit> rayHits)
    {
        return rayHits.Aggregate((u, v) => u.distance < v.distance ? u : v);
    }
}
