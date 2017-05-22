using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TriHorseAnimAudioController : AnimAudioController
{
    public event AnimationEventDelegate OnEatingFinished;
    public event AnimationEventDelegate OnDyingFinished;

    public override void OnStartServer()
    {
        AddExitHandler("Eating", EatingFinished);
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
    public void DoDie()
    {
        //The TriHorse doesn't have a death animation, but we will simulate it
        StartCoroutine(StartDying());
    }

    private IEnumerator StartDying()
    {
        yield return new WaitForSeconds(1f);

        if (OnDyingFinished != null)
            OnDyingFinished();
    }

    private void EatingFinished()
    {
        if (OnEatingFinished != null)
            OnEatingFinished();
    }
}
