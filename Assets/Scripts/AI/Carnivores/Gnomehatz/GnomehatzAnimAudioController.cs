using UnityEngine.Networking;

public class GnomehatzAnimAudioController : AnimAudioController
{
    public event AnimationEventDelegate OnEatingFinished;
    public event AnimationEventDelegate OnAttackFinished;

    public override void OnStartServer()
    {
        AddEnterHandler("LongSwim", LongSwimStart);

        AddExitHandler("Eating", EatingFinished);
        AddExitHandler("Attack", AttackFinished);
    }

    [Server]
    public void DoTurnLeft()
    {
        if (!isClient)
            Anim.SetTrigger("turnLeft");

        RpcTurnLeft();
    }

    [ClientRpc]
    private void RpcTurnLeft()
    {
        Anim.SetTrigger("turnLeft");
    }

    [Server]
    public void DoTurnRight()
    {
        if (!isClient)
            Anim.SetTrigger("turnRight");

        RpcTurnRight();
    }

    [ClientRpc]
    private void RpcTurnRight()
    {
        Anim.SetTrigger("turnRight");
    }

    [Server]
    public void DoTurnDown()
    {
        if (!isClient)
            Anim.SetTrigger("turnDown");

        RpcTurnDown();
    }

    [ClientRpc]
    private void RpcTurnDown()
    {
        Anim.SetTrigger("turnDown");
    }

    [Server]
    public void StartMoveFast()
    {
        if (!isClient)
            Anim.SetBool("moveFast", true);

        RpcStartMoveFast();
    }

    [ClientRpc]
    private void RpcStartMoveFast()
    {
        Anim.SetBool("moveFast", true);
    }

    [Server]
    public void StopMoveFast()
    {
        if (!isClient)
            Anim.SetBool("moveFast", false);

        RpcStopMoveFast();
    }

    [ClientRpc]
    private void RpcStopMoveFast()
    {
        Anim.SetBool("moveFast", false);
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

    private void EatingFinished()
    {
        if (OnEatingFinished != null)
            OnEatingFinished();
    }

    private void AttackFinished()
    {
        if (OnAttackFinished != null)
            OnAttackFinished();
    }

    private void LongSwimStart()
    {
        if (isServer)
            RpcLongSwimStart();
    }

    [ClientRpc]
    private void RpcLongSwimStart()
    {
        if (!isServer && Anim != null)
            Anim.SetTrigger("startLongSwim");
    }
}
