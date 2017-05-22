using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Debug
{
    public class DebugManager : MonoBehaviour
    {
        public bool DebuggingActive;
        public string ActivationToggle;
        public string DebugTag = "Debug";

        private void Start()
        {
            var objs = GetDebugBehaviours();
            //Set their intial state
            foreach (var db in objs)
            {
                SetState(db, DebuggingActive);
            }

            DebugBehaviour.OnNewDebugBehaviour += OnNewDebugBehaviour;
        }

        private void Update()
        {
            if (CrossPlatformInputManager.GetButtonDown(ActivationToggle))
            {
                ToggleDebugBehaviours();
            }
        }

        private void ToggleDebugBehaviours()
        {
            var objs = GetDebugBehaviours();

            //Toggle debug activation
            DebuggingActive = !DebuggingActive;

            foreach (var db in objs)
            {
                SetState(db, DebuggingActive);
            }
        }

        private void SetState(DebugBehaviour db, bool state)
        {
            db.gameObject.SetActive(state);

            //Log a warning if the DebugBehaviour is not tagged correctly.
            //The intended design is to have a separate object contain all debugging related
            //functions so that we can activate/deactivate it to keep debugging
            //code from affecting performance when it isn't desired.
            if (db.gameObject.tag != DebugTag)
                UnityEngine.Debug.LogWarning(db.gameObject.name + " has a DebugBehaviour component but is not tagged as a debug object.");
        }

        private void OnNewDebugBehaviour(DebugBehaviour db)
        {
            db.gameObject.SetActive(DebuggingActive);
        }

        private IEnumerable<DebugBehaviour> GetDebugBehaviours()
        {
            return DebugBehaviour.All;
        }
    }
}
