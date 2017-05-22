using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Debug;
using UnityEngine;

namespace Assets.Scripts.AI.Debug
{
    public class AIDebugDataDisplay : DebugBehaviour
    {
        private readonly List<IAIDebugDataSupplier> m_Suppliers = new List<IAIDebugDataSupplier>();
        public TextMesh TextMesh;
        public GameObject Owner;

        protected/* override */void Start()
        {
            //base.Start();

            if (Owner == null)
                Owner = gameObject;

            m_Suppliers.AddRange(Owner.GetComponentsInChildren<IAIDebugDataSupplier>());
        }

        private void Update()
        {
            var sb = new StringBuilder();

            //Append all the supplied data together.
            foreach (var supplier in m_Suppliers)
            {
                if (supplier == null)
                    continue;
                sb.AppendLine(supplier.GetDebugDataString());
            }

            TextMesh.text = sb.ToString();
        }

        private void OnDisable()
        {
            TextMesh.text = string.Empty;
        }
    }
}
