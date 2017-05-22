using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class Stands : Sculpture
{
    private readonly HashSet<GameObject> m_Seats = new HashSet<GameObject>();

    [SyncVar] public uint Width = 5;
    [SyncVar] public uint Height = 5;
    [SyncVar] public float DistanceBetweenSeats = 1.2f;

    public GameObject SeatPrefab;
    public SyncedSphereCollider InteractableCollider;

    private void Start()
    {
        if (SeatPrefab == null)
            Debug.LogError("SeatPrefab is null on Stands", this);
        if (InteractableCollider == null)
            Debug.LogError("InteractableCollider is null on Stands", this);

        if (!isServer)
            return;

        SpawnSeats();
    }

    private void SpawnSeats()
    {
        var centerOffset = new Vector3(Width / 2f * DistanceBetweenSeats, 0f, 0f);

        var startRot = transform.rotation;
        transform.rotation = Quaternion.identity;
        for (var j = 0; j < Height; j++)
        {
            for (var i = 0; i < Width; i++)
            {
                var pos = new Vector3(i * DistanceBetweenSeats, j * DistanceBetweenSeats, -j * DistanceBetweenSeats);
                var seat = (GameObject)Instantiate(SeatPrefab, pos - centerOffset, Quaternion.identity);
                seat.transform.SetParent(transform, false);

                m_Seats.Add(seat.gameObject);

                NetworkServer.Spawn(seat);

                RpcSetAsChild(seat);
            }
        }
        transform.rotation = startRot;
        InteractableCollider.Radius = GetBoundingSphereRadius();
    }

    private void DestroyAndSpawnSeats()
    {
        foreach (var seat in m_Seats)
        {
            NetworkServer.Destroy(seat);
        }
        m_Seats.Clear();
        SpawnSeats();
    }

    [ClientRpc]
    private void RpcSetAsChild(GameObject seat)
    {
        seat.transform.parent = transform;
    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "Width", () => Width.ToString() },
            { "Height", () => Height.ToString() },
            { "DistanceBetweenSeats", () => DistanceBetweenSeats.ToString() }
        };
    }

    public override float GetBoundingSphereRadius()
    {
        var corner = new Vector3(Width * DistanceBetweenSeats, Height * DistanceBetweenSeats, -Height * DistanceBetweenSeats);
        return corner.magnitude;
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        switch (tokens[1])
        {
            case "Width":
                SetWidth(uint.Parse(tokens[2]));
                return "Set Width to " + tokens[2];
            case "Height":
                SetHeight(uint.Parse(tokens[2]));
                return "Set Height to " + tokens[2];
            case "DistanceBetweenSeats":
                SetDistanceBetweenSeats(float.Parse(tokens[2]));
                return "Set DistanceBetweenSeats to " + tokens[2];
            default:
                return base.RunCommand(cmd, sender);
        }
    }

    private void SetWidth(uint w)
    {
        Width = w;
        DestroyAndSpawnSeats();
    }

    private void SetHeight(uint h)
    {
        Height = h;
        DestroyAndSpawnSeats();
    }

    private void SetDistanceBetweenSeats(float d)
    {
        DistanceBetweenSeats = d;
        DestroyAndSpawnSeats();
    }
}