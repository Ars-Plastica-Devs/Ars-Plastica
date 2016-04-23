using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.Networking;

public class TransparentCubeManager : CubeManager, IDataSupplier
{
    [SyncVar]
    public float CubeInteractionRadius = 20f;

    public GameObject Owner { get { return gameObject; } }

    protected override void Start()
    {
        if (!isServer) return;

        base.Start();

        CalculateColliderAcitvationRange();
    }

    protected override void PreCubeSpawned(GameObject cube)
    {
        cube.GetComponent<MakeTransparent>().enabled = true;
        //cube.GetComponent<SphereCollider>().enabled = false;
        cube.GetComponent<SphereCollider>().enabled = true;
        cube.GetComponent<SphereCollider>().radius = CubeInteractionRadius;
    }

    [Command]
    protected override void CmdActivateBehaviour()
    {
        ActivateBehaviour();
        RpcActivateBehaviour();
    }

    [Command]
    protected override void CmdDeactivateBehaviour()
    {
        DeactivateBehaviour();
        RpcDeactivateBehaviour();
    }

    [ClientRpc]
    protected override void RpcActivateBehaviour()
    {
        ActivateBehaviour();
    }

    [ClientRpc]
    protected override void RpcDeactivateBehaviour()
    {
        DeactivateBehaviour();
    }

    protected override void ActivateBehaviour()
    {
        /*foreach (var cube in Cubes)
        {
            cube.GetComponent<SphereCollider>().enabled = true;
        }*/
    }

    protected override void DeactivateBehaviour()
    {
        /*foreach (var cube in Cubes)
        {
            cube.GetComponent<MakeTransparent>().RpcDeactivateBehaviour();
            cube.GetComponent<SphereCollider>().enabled = false;
        }*/
    }

    protected override void RepositionCubes()
    {
        base.RepositionCubes();

        CalculateColliderAcitvationRange();
    }

    private void CalculateColliderAcitvationRange()
    {
        var cornerCubePosition = Cubes[SideLength].transform.position;
        var distToCorner = (cornerCubePosition - transform.position).magnitude;
        var colliderActivationRange = distToCorner + (Cubes[0].GetComponent<SphereCollider>().radius * Cubes[0].transform.localScale.x);

        GetComponent<SphereCollider>().radius = colliderActivationRange;

        //Disable children colliders so our OverlapSphere call doesn't have to check all of these
        //They can/will/should be re-activated if this object is "Triggered"
        /*foreach (var cube in Cubes)
        {
            cube.GetComponent<SphereCollider>().enabled = false;
        }*/

        var colliders = Physics.OverlapSphere(transform.position, colliderActivationRange).ToList();

        //Remove colliders that don't trigger us.
        for (var i = 0; i < colliders.Count; i++)
        {
            if (colliders[i].gameObject.tag == TriggeringTag) continue;

            colliders.RemoveAt(i);
            i--;
        }

        TriggeredCount = colliders.Count;
        EvaluateTriggeredCount();
    }

    [Command]
    protected virtual void CmdOnChangeCubeInteractRadius(float r)
    {
        CubeInteractionRadius = r;
        UpdateCubeInteractRadius();
    }

    [ClientRpc]
    protected virtual void RpcSetCubeInteractRadius(GameObject cube)
    {
        cube.GetComponent<SphereCollider>().radius = CubeInteractionRadius;
    }

    protected virtual void UpdateCubeInteractRadius()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<SphereCollider>().radius = CubeInteractionRadius;
            RpcSetCubeInteractRadius(cube);
        }
    }

    public override bool IsCommandRelevant(string cmd)
    {
        return cmd.StartsWith(gameObject.name);
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "CubeInteractRadius":
                    var radius = float.Parse(tokens[2]);
                    if (radius < 0)
                    {
                        return "Cannot change CubeInteractRadius to " + radius;
                    }
                    if (radius != CubeInteractionRadius)
                    {
                        CmdOnChangeCubeInteractRadius(radius);
                    }
                    return tokens[0] + "CubeInteractRadius changed to " + radius;
                default:
                    return base.RunCommand(cmd, sender);
            }
        }
        catch
        {
            return "Not a valid command";
        }
    }

    public List<string> GetData()
    {
        return new List<string>();
    }
}
