using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public abstract class PrimitiveWallManager : Sculpture
{
    protected List<GameObject> Primitives = new List<GameObject>(); 

    public float PrimitiveScale;
    public float DistanceBetweenPrimitives;
    public uint Width;
    public uint Height;

    public GameObject Primitive;

    protected virtual void Start()
    {
        if (Primitive == null)
            Debug.LogError("Primitive is null on PrimitiveWallManager", this);

        if (!isServer) return;

        PrimitiveScale = Primitive.transform.localScale.x;
        SpawnPrimitives();
    }

    protected virtual void SpawnPrimitives()
    {
        var centerOffset = new Vector3(DistanceBetweenPrimitives * (Width / 2f), DistanceBetweenPrimitives * (Height / 2f), 0f);

        for (var j = 0; j < Height; j++)
        {
            for (var i = 0; i < Width; i++)
            {
                var pos = new Vector3(DistanceBetweenPrimitives * i, DistanceBetweenPrimitives * j, 0);
                pos -= centerOffset;
                var prim = (GameObject)Instantiate(Primitive, pos, Quaternion.identity);
                prim.transform.localScale = new Vector3(PrimitiveScale, PrimitiveScale, PrimitiveScale);
                prim.transform.SetParent(transform, false);

                PrePrimitiveSpawned(prim);

                Primitives.Add(prim);
                NetworkServer.Spawn(prim);

                RpcSetAsChild(prim);

                PostPrimitiveSpawned(prim);
            }
        }
    }

    protected virtual void RepositionPrimitives()
    {
        var centerOffset = new Vector3(DistanceBetweenPrimitives * (Width / 2f), DistanceBetweenPrimitives * (Height / 2f), 0f);

        for (var j = 0; j < Height; j++)
        {
            for (var i = 0; i < Width; i++)
            {
                var pos = new Vector3(DistanceBetweenPrimitives * i, DistanceBetweenPrimitives * j, 0);
                pos -= centerOffset;
                var prim = Primitives[i + (i * j)];
                prim.transform.localPosition = pos;
                prim.transform.localScale = new Vector3(PrimitiveScale, PrimitiveScale, PrimitiveScale);
            }
        }
    }

    private void DestroyAndSpawnPrimitives()
    {
        foreach (var primitive in Primitives)
        {
            NetworkServer.Destroy(primitive);
        }
        Primitives.Clear();
        SpawnPrimitives();
    }

    [ClientRpc]
    private void RpcSetAsChild(GameObject cube)
    {
        cube.transform.parent = transform;
    }

    [Server]
    protected virtual void SetDistanceBetweenPrimitives(float d)
    {
        if (DistanceBetweenPrimitives == d)
            return;

        DistanceBetweenPrimitives = d;
        DestroyAndSpawnPrimitives();

        RpcSetDistanceBetweenPrimitives(d);
    }

    [ClientRpc]
    private void RpcSetDistanceBetweenPrimitives(float d)
    {
        DistanceBetweenPrimitives = d;
    }

    [Server]
    protected virtual void SetWidth(uint w)
    {
        if (Width == w)
            return;

        Width = w;
        DestroyAndSpawnPrimitives();

        RpcSetWidth(w);
    }

    [Server]
    protected virtual void SetHeight(uint h)
    {
        if (Height == h)
            return;

        Height = h;
        DestroyAndSpawnPrimitives();

        RpcSetHeight(h);
    }

    [ClientRpc]
    private void RpcSetWidth(uint w)
    {
        Width = w;
    }

    [ClientRpc]
    private void RpcSetHeight(uint h)
    {
        Height = h;
    }

    protected virtual void SetPrimitiveScale(float scale)
    {
        if (PrimitiveScale == scale)
            return;

        PrimitiveScale = scale;
        DestroyAndSpawnPrimitives();

        RpcSetPrimitiveScale(scale);
    }

    [ClientRpc]
    private void RpcSetPrimitiveScale(float scale)
    {
        PrimitiveScale = scale;
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "DistanceBetweenPrimitives":
                    SetDistanceBetweenPrimitives(float.Parse(tokens[2]));
                    return tokens[0] + " DistanceBetweenPrimitives changed to " + DistanceBetweenPrimitives;
                case "Width":
                    var width = uint.Parse(tokens[2]);
                    SetWidth(width);
                    return tokens[0] + " Width changed to " + Width;
                case "Height":
                    var height = uint.Parse(tokens[2]);
                    SetHeight(height);
                    return tokens[0] + " Height changed to " + Height;
                case "PrimitiveScale":
                    var scale = float.Parse(tokens[2]);
                    if (scale <= 0)
                    {
                        return "Cannot change PrimitiveScale to " + scale;
                    }
                    SetPrimitiveScale(scale);
                    return tokens[0] + " PrimitiveScale changed to " + PrimitiveScale;
                default:
                    return base.RunCommand(cmd, sender);
            }
        }
        catch
        {
            return "Not a valid command";
        }
    }

    protected abstract void PrePrimitiveSpawned(GameObject obj);
    protected abstract void PostPrimitiveSpawned(GameObject obj);
}
