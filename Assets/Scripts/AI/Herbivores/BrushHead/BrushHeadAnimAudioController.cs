using UnityEngine.Networking;

public class BrushHeadAnimAudioController : AnimAudioController
{
    public event AnimationEventDelegate OnEatingFinished;
    public event AnimationEventDelegate OnDyingFinished;

    public override void OnStartServer()
    {
        AddExitHandler("Eating", EatingFinished);
        AddExitHandler("Die", DieFinished);
    }

    [Server]
    public void DoEat()
    {
        if (!isClient)
            Anim.SetTrigger("eat");

        RpcEat();
    }

    [ClientRpc]
    private void RpcEat()
    {
        Anim.SetTrigger("eat");
    }

    [Server]
    public void DoLookAround()
    {
        if (!isClient)
            Anim.SetTrigger("lookAround");

        RpcLookAround();
    }

    [ClientRpc]
    private void RpcLookAround()
    {
        Anim.SetTrigger("lookAround");
    }

    [Server]
    public void DoDie()
    {
        if (!isClient)
            Anim.SetTrigger("die");

        RpcDie();
    }

    [ClientRpc]
    private void RpcDie()
    {
        if (Anim != null)
            Anim.SetTrigger("die");
    }

    private void EatingFinished()
    {
        if (OnEatingFinished != null)
            OnEatingFinished();
    }

    private void DieFinished()
    {
        if (OnDyingFinished != null)
            OnDyingFinished();
    }
}