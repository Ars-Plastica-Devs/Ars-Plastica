using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class HerbistarAnimAudioController : AnimAudioController
{
    [SyncVar(hook="OnSwitchedMoveAnimation")] private bool m_RollIsActive;

    public event AnimationEventDelegate OnDyingFinished;

    [Server]
    public void SwitchMoveAnimation()
    {
        if (!m_RollIsActive)
        {
            m_RollIsActive = true;
            if (!isClient)
                Anim.SetTrigger("RollMove");
        }
        else
        {
            m_RollIsActive = false;
            if (!isClient)
                Anim.SetTrigger("IdleMove");
        }
    }

    private void OnSwitchedMoveAnimation(bool newVal)
    {
        m_RollIsActive = newVal;

        if (Anim != null)
            Anim.SetTrigger(m_RollIsActive ? "RollMove" : "IdleMove");
    }

    [Server]
    public void DoDie()
    {
        //The DownDown doesn't have a death animation, but we will simulate it
        StartCoroutine(StartDying());
    }

    private IEnumerator StartDying()
    {
        yield return new WaitForSeconds(1f);

        if (OnDyingFinished != null)
            OnDyingFinished();
    }
}
