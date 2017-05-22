using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public class SporeGunNodule : Nodule
{
    private Rigidbody m_Rigidbody;

    [SyncVar] public float Speed;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        m_Rigidbody.velocity = transform.forward * Speed;
    }

    public override void GetEaten(Creature eater)
    {
        Ecosystem.Singleton.RemoveNodule(this);
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer)
            return;

        var other = coll.gameObject;
        if (other.tag != "Structure")
            return;

        Ecosystem.Singleton.RemoveNodule(this);

        var contact = coll.contacts[0];
        Ecosystem.Singleton.SpawnPlant(contact.point, Quaternion.FromToRotation(Vector3.up, contact.normal), PlantType.SporeGun);
    }
}