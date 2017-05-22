using UnityEngine;
using UnityEngine.Networking;

public class SnatcherAnimAudioController : AnimAudioController
{
    public AudioClip YawnSound;
    public AudioClip WardOffSound;
    public AudioClip AttackSound;
    public AudioClip BelchSound;

    public event AnimationEventDelegate OnReleaseSpore;
    public event AnimationEventDelegate OnTongueExtended;
    public event AnimationEventDelegate OnAttackFinished;
    public event AnimationEventDelegate OnDeathFinished;

    public bool Idling {
        get { return Anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"); }
    }

    public override void OnStartClient()
    {
        AddEventHandler("Yawn", PlayYawnSound);
        AddEventHandler("Belch", PlayBelchSound);
        AddEventHandler("WardOffYell", PlayWardOffSound);
        AddEventHandler("AttackSound", PlayAttackSound);
    }

    public override void OnStartServer()
    {
        AddEventHandler("ReleaseSpore", ReleaseSpore);
        AddEventHandler("TongueExtended", TongueExtended);

        AddExitHandler("Attack", AttackFinished);
        AddExitHandler("Death", DeathFinished);
    }

    [Server]
    public void DoAttack()
    {
        if (!isClient)
            Anim.SetTrigger("attack");

        RpcDoAttack();
    }

    [ClientRpc]
    private void RpcDoAttack()
    {
        Anim.SetTrigger("attack");
    }

    [Server]
    public void DoBelch()
    {
        if (!isClient)
            Anim.SetTrigger("belch");

        RpcDoBelch();
    }

    [ClientRpc]
    private void RpcDoBelch()
    {
        Anim.SetTrigger("beltch");
    }

    [Server]
    public void DoWardOff()
    {
        if (!isClient)
            Anim.SetTrigger("wardOff");

        RpcDoWardOff();
    }

    [ClientRpc]
    private void RpcDoWardOff()
    {
        Anim.SetTrigger("wardOff");
    }

    [Server]
    public void DoDeath()
    {
        if (!isClient)
            Anim.SetTrigger("death");

        RpcDoDeath();
    }

    [ClientRpc]
    private void RpcDoDeath()
    {
        Anim.SetTrigger("death");
    }

    public void DoIdleAction()
    {
        //here is where the possibility of extra actions is determined
        var extraAction = Random.Range(0f, 100f);
        if (extraAction < 30f)
        {
            if (!isClient)
                Anim.SetTrigger("yawn");
            
            RpcDoYawn();
        }
        /*if(extraAction > 90f)
        {
            m_Anim.SetTrigger("beltch");
            m_AudioSource.PlayOneShot(BelchSound);
        }*/
    }

    [ClientRpc]
    private void RpcDoYawn()
    {
        Anim.SetTrigger("yawn");
    }

    private void ReleaseSpore()
    {
        if (OnReleaseSpore != null)
            OnReleaseSpore();
    }

    private void TongueExtended()
    {
        if (OnTongueExtended != null)
            OnTongueExtended();
    }

    private void AttackFinished()
    {
        if (OnAttackFinished != null)
            OnAttackFinished();
    }

    private void DeathFinished()
    {
        if (OnDeathFinished != null)
            OnDeathFinished();
    }

    private void PlayAttackSound()
    {
        if (isClient)
            AudioSource.PlayOneShot(AttackSound);
    }

    private void PlayBelchSound()
    {
        if (isClient)
            AudioSource.PlayOneShot(BelchSound);
    }

    private void PlayYawnSound()
    {
        if (isClient)
            AudioSource.PlayOneShot(YawnSound);
    }

    private void PlayWardOffSound()
    {
        if (isClient)
            AudioSource.PlayOneShot(WardOffSound);
    }
}
