using UnityEngine.Networking;

public class FellyJishAnimAudioController : AnimAudioController
{
    public event AnimationEventDelegate OnAttackFinished;
    public event AnimationEventDelegate OnEatingFinished;

    public override void OnStartServer()
    {
        AddExitHandler("Eating", EatingFinished);
        AddExitHandler("Attack", AttackFinished);
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
    public void DoEating()
    {
        if (!isClient)
            Anim.SetTrigger("eating");

        RpcDoEating();
    }

    [ClientRpc]
    private void RpcDoEating()
    {
        Anim.SetTrigger("eating");
    }

    private void AttackFinished()
    {
        if (OnAttackFinished != null)
            OnAttackFinished();
    }

    private void EatingFinished()
    {
        if (OnEatingFinished != null)
            OnEatingFinished();
    }
}
