using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.NoiseGeneration;
using Assets.Scripts.Sculpture.Behaviours;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Sculpture.PrimitiveWallManagers
{
    public class AnimatedNoiseWall : CubeWallManager
    {
        private List<NoiseCube> m_NoiseCubes = new List<NoiseCube>();
        private float[,,] m_Noise;
        private int m_AnimPoint;
        private int m_Direction = 1;
        [SerializeField]
        private float m_AnimRate = .5f;
        [SerializeField]
        private float m_SpeedFactor = 1f;
        [SerializeField]
        private float m_DistanceScale = 10f;

        [SyncVar]
        [SerializeField]
        private float m_CubeSize;

        public override float CubeSize
        {
            get { return m_CubeSize; }
            set { m_CubeSize = value; }
        }

        protected override void Start()
        {
            m_NoiseCubes = new List<NoiseCube>((int) (SideLength * SideLength));

            base.Start();

            m_Noise = FractalBrownianMotion.GenerateTileableNoise((int) SideLength, (int) SideLength, 20, 3, .55f, .05f,
                2f, SimplexNoiseGenerator.Noise3D, (int) (Random.value * 10000f));

            StartCoroutine(ChangeAnimPoint());
        }

        private IEnumerator ChangeAnimPoint()
        {
            while (true)
            {
                m_AnimPoint += m_Direction;
                if (m_AnimPoint == 19 || m_AnimPoint == 0)
                    m_Direction *= -1;
                yield return new WaitForSeconds(m_AnimRate);
            }
        }

        protected override void PreCubeSpawned(GameObject cube, int x, int y)
        {
            if (m_NoiseCubes.Count <= (x + (SideLength * y)))
                m_NoiseCubes.Add(cube.GetComponent<NoiseCube>());
            else
                m_NoiseCubes[(int) (x + (SideLength * y))] = cube.GetComponent<NoiseCube>();

            cube.GetComponent<NoiseCube>().ParentNetID = netId;
        }

        protected override void PostCubeSpawned(GameObject cube)
        {
            cube.GetComponent<NoiseCube>().Initialize();
        }

        private void Update()
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    var target = Mathf.Lerp(m_NoiseCubes[(int) (i + (SideLength * j))].TargetZ,
                        m_Noise[i, j, m_AnimPoint] * m_DistanceScale, Time.deltaTime * m_SpeedFactor);
                    m_NoiseCubes[(int) (i + (SideLength * j))].TargetZ = target;
                }
            }
        }

        public override Dictionary<string, Func<string>> GetCurrentData()
        {
            return new Dictionary<string, Func<string>>
        {
            { "GapFactor", () => GapFactor.ToString() },
            { "SideLength", () => SideLength.ToString() },
            { "CubeSize", () => CubeSize.ToString() }
        };
        }
    }
}
