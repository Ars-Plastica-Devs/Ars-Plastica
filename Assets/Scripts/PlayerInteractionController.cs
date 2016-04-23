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

    public void SitOnConveyance(GameObject obj)
    {
        m_ConveyanceAttachedTo = obj;
        CheckForInteractions = false;
        DeactivateGameInputExceptMouse();
        m_ConveyanceAttachedTo.GetComponent<ConveyanceController>().AttachObject(gameObject);
        CmdSitOnConveyance(m_ConveyanceAttachedTo);
    }

    [Command]
    private void CmdSitOnConveyance(GameObject obj)
    {
        obj.GetComponent<ConveyanceController>().AttachObject(gameObject);
        obj.GetComponent<ConveyanceController>().ServerStartRunning();
    }

    public void DetachFromConveyance()
    {
        m_InteractHandler.Active = false;
        m_ConveyanceAttachedTo.GetComponent<ConveyanceController>().DetachObject(gameObject);
        EnableGameInput();
        CmdDetachFromConveyance(m_ConveyanceAttachedTo);

        CheckForInteractions = false;
        m_ConveyanceAttachedTo = null;
    }

    [Command]
    private void CmdDetachFromConveyance(GameObject obj)
    {
        obj.GetComponent<ConveyanceController>().DetachObject(gameObject);
    }

    private static RaycastHit GetClosestRayHit(IEnumerable<RaycastHit> rayHits)
    {
        return rayHits.Aggregate((u, v) => u.distance < v.distance ? u : v);
    }
}
