﻿using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

/*
 * Modified Standard Asset FirstPersonController to add flying ability.
 * Flying is accomplished by 'Ascend' and 'Descend' key bindings, make sure those are registered.
 * */
[RequireComponent(typeof (CharacterController))]
[RequireComponent(typeof (AudioSource))]
public class FirstPersonController : MonoBehaviour
{
	[SerializeField] private bool m_IsWalking;
	[SerializeField] private float m_WalkSpeed;
	[SerializeField] private float m_SprintMultiplier;
	[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
	[SerializeField] private float m_JumpSpeed;
	[SerializeField] private float m_StickToGroundForce;
	[SerializeField] private float m_GravityMultiplier;
	[SerializeField] private MouseLook m_MouseLook;
	[SerializeField] private bool m_UseFovKick;
	[SerializeField] private FOVKick m_FovKick = new FOVKick();
	[SerializeField] private bool m_UseHeadBob;
	[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
	[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
	[SerializeField] private float m_StepInterval;
	[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
	[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
	[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
	//Flying Stuff
	[SerializeField] private bool m_flying;
	[SerializeField] private bool m_ascending;
	[SerializeField] private bool m_descending;
	[SerializeField] private float m_AscendSpeed;
	[SerializeField] private float m_DescendSpeed;
	[SerializeField] private float m_FlySpeed;


	private Camera m_Camera;
	private bool m_Jump;
	private float m_YRotation;
	private Vector2 m_Input;
	private Vector3 m_MoveDir = Vector3.zero;
	private CharacterController m_CharacterController;
	private CollisionFlags m_CollisionFlags;
	private bool m_PreviouslyGrounded;
	private Vector3 m_OriginalCameraPosition;
	private float m_StepCycle;
	private float m_NextStep;
	private bool m_Jumping;
	private AudioSource m_AudioSource;

	private bool m_PreviouslyFlying; //were we flying last update

    public bool MouseOnly = false;

	// Use this for initialization
	private void Start()
	{
		m_CharacterController = GetComponent<CharacterController>();
		m_Camera = Camera.main;
		m_OriginalCameraPosition = m_Camera.transform.localPosition;
		m_FovKick.Setup(m_Camera);
		m_HeadBob.Setup(m_Camera, m_StepInterval);
		m_StepCycle = 0f;
		m_NextStep = m_StepCycle/2f;
		m_Jumping = false;
		m_AudioSource = GetComponent<AudioSource>();
		m_MouseLook.Init(transform , m_Camera.transform);
		//added flying flags
		m_flying = false;
		m_descending = false;
		m_ascending = false;
	}


	// Update is called once per frame
	private void Update()
	{
		RotateView();

	    if (MouseOnly)
	        return;

		// the jump state needs to read here to make sure it is not missed
		if (!m_Jump)
		{
			m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
		}
		
		m_ascending = CrossPlatformInputManager.GetButton ("Ascend");
		m_descending = CrossPlatformInputManager.GetButton ("Descend");

        if (m_ascending || m_descending) {
			m_flying = true;
			m_PreviouslyFlying = true;
		}
			
		//cancel flying
		if (m_Jump) {
			m_PreviouslyFlying = false;
		}

		//reset jump to false if we weren't on the ground
		if (!m_CharacterController.isGrounded) {
			m_Jump = false;
		} else {
			m_flying = false;
		}

		if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
		{
			StartCoroutine(m_JumpBob.DoBobCycle());
			PlayLandingSound();
			m_MoveDir.y = 0f;
			m_Jumping = false;
			m_flying = false;
			m_PreviouslyFlying = false;
		}
			
		if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
		{
			m_MoveDir.y = 0f;
		}
			
		m_PreviouslyGrounded = m_CharacterController.isGrounded;
	}


    public Vector3 GetLookDirection()
    {
        return m_Camera.transform.forward;
    }

    public Vector3 GetCameraLocation()
    {
        return m_Camera.transform.position;
    }

    private void PlayLandingSound()
	{
		if (m_LandSound) {
			m_AudioSource.clip = m_LandSound;
			m_AudioSource.Play ();
			m_NextStep = m_StepCycle + .5f;
		}
	}


	private void FixedUpdate()
	{
		float speed;
		GetInput(out speed);
		// always move along the camera forward as it is the direction that it being aimed at
		Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

		// get a normal for the surface that is being touched to move along it
		RaycastHit hitInfo;
		Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
			m_CharacterController.height/2f, ~0, QueryTriggerInteraction.Ignore);
		desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

		m_MoveDir.x = desiredMove.x*speed;
		m_MoveDir.z = desiredMove.z*speed;


		//Added flying check
		if (m_CharacterController.isGrounded  && !m_ascending) {
			
			m_MoveDir.y = -m_StickToGroundForce;

			if (m_Jump) {
				m_MoveDir.y = m_JumpSpeed;
				PlayJumpSound ();
				m_Jump = false;
				m_Jumping = true;
			}
		} else {
			//Move in correct direction based on flags set.
			if (m_ascending) {
				if(m_MoveDir.y < 0) m_MoveDir.y = 0;
				m_MoveDir += transform.up * m_AscendSpeed * Time.fixedDeltaTime;
			} else if (m_descending) {
				m_MoveDir -= transform.up * m_DescendSpeed * Time.fixedDeltaTime;
			} else if (!m_PreviouslyFlying) {
				m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
			} else {
				//if no flags set, we are flying, but not ascending/descending
				//lerp to smooth stop (up/down only)
				float diff = 0 - m_MoveDir.y;
				if (Math.Abs (diff) < 1) {
					m_MoveDir.y = 0f;
				} else {
					m_MoveDir.y += diff * Time.fixedDeltaTime;
				}
			}
				
		}
		m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

		ProgressStepCycle(speed);
		UpdateCameraPosition(speed);

		m_MouseLook.UpdateCursorLock();
	}


	private void PlayJumpSound()
	{
		m_AudioSource.clip = m_JumpSound;
		m_AudioSource.Play();
	}


	private void ProgressStepCycle(float speed)
	{
		if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
		{
			m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
				Time.fixedDeltaTime;
		}

		if (!(m_StepCycle > m_NextStep))
		{
			return;
		}

		m_NextStep = m_StepCycle + m_StepInterval;

//		PlayFootStepAudio();
	}


	private void PlayFootStepAudio()
	{
		if (!m_CharacterController.isGrounded)
		{
			return;
		}
		// pick & play a random footstep sound from the array,
		// excluding sound at index 0
		int n = Random.Range(1, m_FootstepSounds.Length);
		m_AudioSource.clip = m_FootstepSounds[n];
		m_AudioSource.PlayOneShot(m_AudioSource.clip);
		// move picked sound to index 0 so it's not picked next time
		m_FootstepSounds[n] = m_FootstepSounds[0];
		m_FootstepSounds[0] = m_AudioSource.clip;
	}


	private void UpdateCameraPosition(float speed)
	{
		Vector3 newCameraPosition;
		if (!m_UseHeadBob)
		{
			return;
		}
		if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
		{
			m_Camera.transform.localPosition =
				m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
					(speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
			newCameraPosition = m_Camera.transform.localPosition;
			newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
		}
		else
		{
			newCameraPosition = m_Camera.transform.localPosition;
			newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
		}
		m_Camera.transform.localPosition = newCameraPosition;
	}


	private void GetInput(out float speed)
	{
		// Read input
		float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
		float vertical = CrossPlatformInputManager.GetAxis("Vertical");


		bool waswalking = m_IsWalking;

		#if !MOBILE_INPUT
		// On standalone builds, walk/run speed is modified by a key press.
		// keep track of whether or not the character is walking or running
		m_IsWalking = !CrossPlatformInputManager.GetButton("Sprint");
		#endif
		// set the desired speed to be walking or running

		speed = m_flying ? m_FlySpeed : m_WalkSpeed;

        if (!m_IsWalking) speed *= m_SprintMultiplier;

        m_Input = new Vector2(horizontal, vertical);

		// normalize input if it exceeds 1 in combined length:
		if (m_Input.sqrMagnitude > 1)
		{
			m_Input.Normalize();
		}

		// handle speed change to give an fov kick
		// only if the player is going to a run, is running and the fovkick is to be used
		if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
		{
			StopAllCoroutines();
			StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
		}
	}


	private void RotateView()
	{
		m_MouseLook.LookRotation (transform, m_Camera.transform);
	}


	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody body = hit.collider.attachedRigidbody;
		//dont move the rigidbody if the character is on top of it
		if (m_CollisionFlags == CollisionFlags.Below)
		{
			return;
		}

		if (body == null || body.isKinematic)
		{
			return;
		}
		body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
	}

    private void OnDisable()
    {
        m_MouseLook.SetCursorLock(false);
    }

    private void OnEnable()
    {
        m_MouseLook.SetCursorLock(true);
    }
}

