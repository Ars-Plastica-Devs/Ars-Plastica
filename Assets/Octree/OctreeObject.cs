using System.Collections;
using UnityEngine;

namespace Assets.Octree
{
    /// <summary>
    /// Handles insertion and removal from an Octree of the given type
    /// </summary>
    public class OctreeObject : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        private bool _inOctree;
        private bool m_InOctree
        {
            set
            {
                if (_inOctree == value)
                    return;

                var octree = OctreeManager.Get(Type);

                if (value)
                {
                    if (octree != null)
                    {
                        StopAllCoroutines();
                        octree.Add(transform);
                        _inOctree = true;
                    }
                    else
                        StartCoroutine(WaitToJoinOctree());
                }
                else
                {
                    StopAllCoroutines();
                    if (octree != null)
                        octree.Remove(transform);
                    _inOctree = false;
                }
                
            }
        }
        public OctreeType Type;

        private void Start()
        {
            m_InOctree = true;
        }

        private void OnEnable()
        {
            m_InOctree = true;
        }

        private void OnDisable()
        {
            m_InOctree = false;
        }

        private void OnDestroy()
        {
            m_InOctree = false;
        }

        private IEnumerator WaitToJoinOctree()
        {
            Octree octree;
            while ((octree = OctreeManager.Get(Type)) == null)
            {
                yield return 0;
            }

            octree.Add(transform);
            _inOctree = true;
        }
    }
}
