using UnityEngine;

public class OneTimeParticleController : MonoBehaviour
{
    private float m_Lifetime;
    public ParticleSystem System;

    private void Start()
    {
        m_Lifetime = System.main.duration + System.main.startLifetime.constantMin;
    }

    private void Update()
    {
        m_Lifetime -= Time.deltaTime;
        if (m_Lifetime > 0f)
            return;
        
        Destroy(gameObject);
    }
}
