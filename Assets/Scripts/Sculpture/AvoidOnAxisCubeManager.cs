using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class AvoidOnAxisCubeManager : CubeManager
{
    public AvoidOnAxis.AvoidParameters AvoidParams = new AvoidOnAxis.AvoidParameters(6f, 6f, 18f, 10f, .4f);
    //public const bool IndividualizedCubes = true;
    [SyncVar]
    public float CubeInteractionRadius = 20f;

    protected override void Start ()
	{
        base.Start();

        CalculateColliderAcitvationRange();
    }

    protected override void PreCubeSpawned(GameObject cube)
    {
        cube.GetComponent<AvoidOnAxis>().enabled = true;
        cube.GetComponent<AvoidOnAxis>().AvoidParams = AvoidParams;
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
            cube.GetComponent<AvoidOnAxis>().DeactivateBehaviour();
            cube.GetComponent<SphereCollider>().enabled = false;
        }*/
    }

    protected override void RepositionCubes()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().Reset();
        }

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
                    if (radius != Cubes[0].GetComponent<SphereCollider>().radius)
                    {
                        CmdOnChangeCubeInteractRadius(radius);
                    }
                    return tokens[0] + "CubeInteractRadius changed to " + radius;
                //NOTE: Removed IndividualizedCubes toggling for now. Unlikely to use and non trivial to change at runtime
                /*case "IndividualizedCubes":
                    var oldInd = IndividualizedCubes;
                    //IndividualizedCubes = bool.Parse(tokens[2]);
                    if (oldInd != IndividualizedCubes) OnIndividualizedCubesChanged();
                    break;*/
                case "DistanceToTravel":
                    var oldDist = AvoidParams.DistanceToTravel;
                    AvoidParams.DistanceToTravel = float.Parse(tokens[2]);
                    if (AvoidParams.DistanceToTravel != oldDist) CmdOnDistanceToTravelChanged();
                    return tokens[0] + " DistanceToTravel changed to " + AvoidParams.DistanceToTravel;
                case "DistanceVariation":
                    var oldDistVar = AvoidParams.DistanceVariation;
                    AvoidParams.DistanceVariation = float.Parse(tokens[2]);
                    if (AvoidParams.DistanceVariation != oldDistVar) CmdOnDistanceVariationChanged();
                    return tokens[0] + " DistanceVariation changed to " + AvoidParams.DistanceVariation;
                case "Speed":
                    var oldSpeed = AvoidParams.Speed;
                    AvoidParams.Speed = float.Parse(tokens[2]);
                    if (AvoidParams.Speed < 0)
                    {
                        var badSpeed = AvoidParams.Speed;
                        AvoidParams.Speed = oldSpeed;
                        return "Cannot changed Speed to " + badSpeed;
                    }
                    if (AvoidParams.Speed != oldSpeed) CmdOnSpeedChanged();
                    return tokens[0] + " Speed changed to " + AvoidParams.Speed;
                case "SpeedVariation":
                    var oldSpeedVar = AvoidParams.SpeedVariation;
                    AvoidParams.SpeedVariation = float.Parse(tokens[2]);
                    if (AvoidParams.SpeedVariation != oldSpeedVar) CmdOnSpeedVariationChanged();
                    return tokens[0] + " SpeedVariation changed to " + AvoidParams.SpeedVariation;
                default:
                    return base.RunCommand(cmd, sender);
            }
        }
        catch
        {
            return "Not a valid command";
        }
    }

    //The following Commands do not need to sync data to clients
    //beacuse those clients do not use this data - it's only used
    //for the AvoidOnAxis logic which runs server side

    [Command]
    private void CmdOnSpeedVariationChanged()
    {
        OnSpeedVariationChanged();
    }

    private void OnSpeedVariationChanged()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.SpeedVariation = AvoidParams.SpeedVariation;
        }
    }

    [Command]
    private void CmdOnSpeedChanged()
    {
        OnSpeedChanged();
    }

    private void OnSpeedChanged()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.Speed = AvoidParams.Speed;
        }
    }

    [Command]
    private void CmdOnDistanceVariationChanged()
    {
        OnDistanceVariationChanged();
    }

    private void OnDistanceVariationChanged()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.DistanceVariation = AvoidParams.DistanceVariation;
        }
    }

    [Command]
    private void CmdOnDistanceToTravelChanged()
    {
        OnDistanceToTravelChanged();
    }

    private void OnDistanceToTravelChanged()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.DistanceToTravel = AvoidParams.DistanceToTravel;
        }
    }
}
