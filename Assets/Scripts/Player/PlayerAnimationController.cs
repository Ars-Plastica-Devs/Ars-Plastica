using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 3, sendInterval = 0.2f)]
[RequireComponent(typeof(FirstPersonController))]
public class PlayerAnimationController : AnimAudioController
{
    private Vector3 m_LastVelocity;
    private bool m_Grounded = true;
    private FirstPersonController m_Controller;

    public Transform Legs;
    public Vector3 EulerLegsRotOffset;

    private void Start()
    {
        m_LastVelocity = transform.forward;

        m_Controller = GetComponent<FirstPersonController>();

        m_Controller.OnJump += OnJump;
        m_Controller.OnLand += OnLand;
        m_Controller.OnStartFlying += OnStartFlying;
        //m_Controller.OnStartRunning += OnStartRunning;
        //m_Controller.OnStopRunning += OnStopRunning;
        m_Controller.OnMove += OnMove;
    }

    private void LateUpdate()
    {
        if (!m_Grounded)
        {
            Legs.rotation = transform.rotation * Quaternion.Euler(EulerLegsRotOffset);
            return;
        }

        var velRot = Quaternion.LookRotation(m_LastVelocity, transform.up) * Quaternion.Euler(EulerLegsRotOffset);
        Legs.rotation = Quaternion.Slerp(Legs.rotation, velRot, 10f * Time.deltaTime);
    }

    private void OnJump()
    {
        //Debug.Log("Jumped");
        Anim.SetTrigger("Jump");
        m_Grounded = false;
        CmdOnJump();
    }

    private void OnLand()
    {
        //Debug.Log("Landed");
        Anim.SetBool("Flying", false);
        m_Grounded = true;
        CmdOnLand();
    }

    private void OnStartFlying()
    {
        //Debug.Log("Start Flying");
        Anim.SetBool("Flying", true);
        m_Grounded = false;
        CmdOnStartFlying();
    }

    /*private void OnStartRunning()
    {
        //Debug.Log("Started Moving");
    }

    private void OnStopRunning()
    {
        //Debug.Log("Stopped Moving");
    }*/

    private void OnMove(Vector3 vel)
    {
        //Zero out vertical velocity - animation does not depend on that
        vel = new Vector3(vel.x, 0f, vel.z);
        Anim.SetFloat("MoveSpeed", vel.magnitude);
        CmdOnMove(vel);
        if (vel.magnitude > 0f)
            m_LastVelocity = vel;
    }

    [Command]
    private void CmdOnJump()
    {
        RpcOnJump();
    }

    [ClientRpc]
    private void RpcOnJump()
    {
        if (isLocalPlayer || Anim == null) return;
        Anim.SetTrigger("Jump");
    }

    [Command]
    private void CmdOnLand()
    {
        RpcOnLand();
    }

    [ClientRpc]
    private void RpcOnLand()
    {
        if (isLocalPlayer || Anim == null) return;
        Anim.SetBool("Flying", false);
    }

    [Command]
    private void CmdOnStartFlying()
    {
        RpcOnStartFlying();
    }

    [ClientRpc]
    private void RpcOnStartFlying()
    {
        if (isLocalPlayer || Anim == null) return;
        Anim.SetBool("Flying", true);
    }

    [Command]
    private void CmdOnMove(Vector3 vel)
    {
        RpcOnMove(vel);
    }

    [ClientRpc]
    private void RpcOnMove(Vector3 vel)
    {
        if (isLocalPlayer || Anim == null) return;
        //Zero out vertical velocity - animation does not depend on that
        vel = new Vector3(vel.x, 0f, vel.z);
        Anim.SetFloat("MoveSpeed", vel.magnitude);

        if (vel.magnitude > 0f)
            m_LastVelocity = vel;
    }

    /*private void RotateLegs(Vector3 vel)
    {
        if (!m_Grounded || vel.magnitude == 0f)
            return;

        var velRot = Quaternion.LookRotation(vel, transform.up) * Quaternion.Euler(EulerLegsRotOffset);
        Legs.rotation = velRot;
    }*/
}
