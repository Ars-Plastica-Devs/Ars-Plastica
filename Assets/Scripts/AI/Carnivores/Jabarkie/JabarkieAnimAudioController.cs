using UnityEngine;
using UnityEngine.Networking;

public class JabarkieAnimAudioController : AnimAudioController
{
    public AudioClip ChirpSound;
    public AudioClip ScreamSoundOne;
    public AudioClip ScreamSoundTwo;
    public event AnimationEventDelegate OnRoarFinished;
    public event AnimationEventDelegate OnDiveBombFinished;
    public event AnimationEventDelegate OnStrikeFinished;
    public event AnimationEventDelegate OnEatingFinished;

    public override void OnStartClient()
    {
        AddEventHandler("Roar", PlayScreamSound);
    }

    public override void OnStartServer()
    {
        AddEnterHandler("FlapFlight", StartFlight);

        AddExitHandler("Eating", EatingFinished);
        AddExitHandler("Strike", StrikeFinished);
        AddExitHandler("DiveBomb", DiveBombFinished);
        AddExitHandler("Roar", RoarFinished);
    }

    [Server]
    public void DoTurnLeft()
    {
        if (!isClient)
            Anim.SetTrigger("leftTurn");

        RpcTurnLeft();
    }

    [ClientRpc]
    private void RpcTurnLeft()
    {
        Anim.SetTrigger("leftTurn");
    }

    [Server]
    public void DoTurnRight()
    {
        if (!isClient)
            Anim.SetTrigger("rightTurn");

        RpcTurnRight();
    }

    [ClientRpc]
    private void RpcTurnRight()
    {
        Anim.SetTrigger("rightTurn");
    }

    [Server]
    public void DoQuickStopAndRoar()
    {
        if (!isClient)
            Anim.SetTrigger("quickStopAndRoar");

        RpcQuickStopAndRoar();
    }

    [ClientRpc]
    private void RpcQuickStopAndRoar()
    {
        Anim.SetTrigger("quickStopAndRoar");
    }

    [Server]
    public void DoDiveBomb()
    {
        if (!isClient)
            Anim.SetTrigger("diveBomb");

        RpcDiveBomb();
    }

    [ClientRpc]
    private void RpcDiveBomb()
    {
        Anim.SetTrigger("diveBomb");
    }

    [Server]
    public void DoStrike()
    {
        if (!isClient)
            Anim.SetTrigger("strike");

        RpcStrike();
    }


    [ClientRpc]
    private void RpcStrike()
    {
        Anim.SetTrigger("strike");
    }

    private void DiveBombFinished()
    {
        if (OnDiveBombFinished != null)
            OnDiveBombFinished();
    }

    private void StrikeFinished()
    {
        if (OnStrikeFinished != null)
            OnStrikeFinished();
    }

    private void RoarFinished()
    {
        if (OnRoarFinished != null)
            OnRoarFinished();
    }

    private void EatingFinished()
    {
        if (OnEatingFinished != null)
            OnEatingFinished();
    }

    private void PlayScreamSound()
    {
        if (Random.value < .5f && ScreamSoundOne != null)
            AudioSource.PlayOneShot(ScreamSoundOne);
        else if (ScreamSoundTwo != null)
            AudioSource.PlayOneShot(ScreamSoundTwo);
    }

    private void StartFlight()
    {
        if (isServer)
            RpcStartFlight();
    }

    [ClientRpc]
    private void RpcStartFlight()
    {
        if (!isServer && Anim != null)
            Anim.SetTrigger("startFlapFlight");
    }
}