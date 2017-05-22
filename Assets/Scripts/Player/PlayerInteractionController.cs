using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerInteractionController : NetworkBehaviour
{
    private HUDManager m_HUDManager;
    private InteractionHandler m_InteractHandler;
    private FirstPersonController m_Controller;
    private GameObject m_ConveyanceAttachedTo;

    private int m_CheckCounter;
    private const int CHECK_RATE = 4;

    public float InteractDistance = 5f;
    public float InfoDistance = 10f;
    public bool InteractInputEnabled = true;
    public bool CheckForInteractions = true;

    public LayerMask InteractableLayerMask;
    public LayerMask InfoLayerMask;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }
        m_HUDManager = HUDManager.Singleton;
        m_Controller = GetComponent<FirstPersonController>();
    }

    public void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyUp(KeyCode.F) && InteractInputEnabled)
        {
            OnInteract();
        }

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Camera.main.ViewportToScreenPoint(new Vector3(.5f, .5f, 0f)));

            OnClickCheck(ray.origin, ray.direction);

            if (!isServer) //Host doesn't need to do this twice
                CmdOnClickCheck(ray.origin, ray.direction);
        }

        m_CheckCounter++;
        
        if (m_CheckCounter > CHECK_RATE)
        {
            m_CheckCounter = 0;
            DoInfoSupplierCheck();

            if (CheckForInteractions)
                DoInteractableCheck();
        }
    }

    private void OnInteract()
    {
        if (m_InteractHandler == null) return;
        m_InteractHandler.OnInteract(this);
    }

    //Sets InteractionHandlers active/inactive as required
    private void DoInteractableCheck()
    {
        //TODO: Convoluted - revise this method
        var rayHits = Physics.SphereCastAll(m_Controller.GetCameraLocation(), 1f, m_Controller.GetLookDirection(), InteractDistance, InteractableLayerMask, QueryTriggerInteraction.Collide);

        rayHits = rayHits.Where(rh => rh.collider.gameObject.GetComponent<InteractionHandler>() != null).ToArray();

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
        var interactable = closest.collider.gameObject.GetComponent<InteractionHandler>();

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

    private void OnClickCheck(Vector3 pos, Vector3 dir)
    {
        var hits = Physics.RaycastAll(pos, dir, InteractDistance, InteractableLayerMask);

        for (var i = 0; i < hits.Length; i++)
        {
            if (DoOnClickCheck(hits[i].collider.gameObject))
                break;
        }
    }

    [Command]
    private void CmdOnClickCheck(Vector3 pos, Vector3 dir)
    {
        OnClickCheck(pos, dir);
    }

    //Calls the interact function of any relevant ClickInteractionHandlers
    private bool DoOnClickCheck(GameObject obj)
    {
        var interactable = obj.GetComponent<ClickInteractionHandler>();

        if (interactable == null 
            || (interactable.ServerSide && !isServer) 
            || (!interactable.ServerSide && (isServer && !isClient)))
            return false;

        interactable.OnInteract(this, true);
        return true;
    }

    private void DoInfoSupplierCheck()
    {
        var rayHits = Physics.SphereCastAll(m_Controller.GetCameraLocation(), 1f, m_Controller.GetLookDirection(), InfoDistance, InfoLayerMask, QueryTriggerInteraction.Collide).ToList();
        rayHits.RemoveAll(rh => rh.transform.gameObject.GetComponent<InfoSupplier>() == null 
                                && rh.transform.gameObject.GetComponent<ConstantLocalInfoSupplier>() == null);

        if (rayHits.Count == 0)
        {
            //m_HUDManager.SetInfoText(string.Empty);
            return;
        }
        
        var closest = GetClosestRayHit(rayHits);
        var dataSupplier = closest.transform.gameObject;

        var constDataSupplier = dataSupplier.GetComponentInChildren<ConstantLocalInfoSupplier>();
        if (constDataSupplier != null)
        {
            m_HUDManager.SetInfoText(constDataSupplier.ConstantInfo);
            return;
        }

        CmdGetDataSupplied(dataSupplier, NetworkClient.allClients[0].connection.connectionId);
    }

    [Command]
    private void CmdGetDataSupplied(GameObject supplier, int conIDOfSender)
    {
        if (supplier == null || supplier.GetComponent<InfoSupplier>() == null)
            return;

        var data = supplier.GetComponent<InfoSupplier>().GetDataString();
        RpcDataSupplied(data, conIDOfSender);
    }

    [ClientRpc]
    private void RpcDataSupplied(string data, int conIDOfReceiver)
    {
        if (conIDOfReceiver != NetworkClient.allClients[0].connection.connectionId 
            || m_HUDManager == null 
            || data == null)
            return;

        m_HUDManager.SetInfoText(data);
    }

    public void DeactivateGameInput()
    {
        if (!isLocalPlayer) return;
        m_Controller.MouseOnly = false;
        m_Controller.LockMouse(false);
    }

    public void DeactivateGameInputExceptMouse()
    {
        if (!isLocalPlayer) return;
        m_Controller.MouseOnly = true;
    }

    public void EnableGameInput()
    {
        if (!isLocalPlayer) return;
        m_Controller.MouseOnly = false;
        m_Controller.LockMouse(true);
    }

    public void RequestSeatOnConveyance(string startBeaconName, string endBeaconName)
    {
        CmdRequestSeatOnConveyance(startBeaconName, endBeaconName);

        //Deactivate these right away so we arent waiting for response from server
        CheckForInteractions = false;
        DeactivateGameInputExceptMouse();
    }

    [ClientRpc]
    private void RpcSitOnConveyance(GameObject con)
    {
        if (!isLocalPlayer) return;

        m_ConveyanceAttachedTo = con;
        GetComponentInChildren<AudioListener>().enabled = false;
        m_InteractHandler = m_ConveyanceAttachedTo.GetComponent<InteractionHandler>();
    }

    [Command]
    private void CmdRequestSeatOnConveyance(string startBeaconName, string endBeaconName)
    {
        //Spawn the conveyance, then call our RPC
        var pathManager = GameObject.FindGameObjectWithTag("PathManager").GetComponent<PathManager>();
        var obj = pathManager.SpawnConveyance(startBeaconName, endBeaconName);
        var con = obj.GetComponent<ConveyanceController>();

        pathManager.NotifyOfAttachToConveyance(this, con);
        RpcSitOnConveyance(obj);
    }

    public void DetachFromConveyance()
    {
        if (isLocalPlayer)
            GetComponentInChildren<AudioListener>().enabled = true;

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
