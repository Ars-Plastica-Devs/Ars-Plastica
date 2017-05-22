using System;
using UnityEngine;

namespace Assets.Scripts.Animation
{
    [Serializable]
    public class AnimatorEvent
    {
        [HideInInspector]
        public int NameHash; //Set from the relevant PropertyDrawer
        public string EventName;
        [Tooltip("The normalized time at which this event should be fired.")]
        public float EventTime;
        [Tooltip("Determines whether or not this event will be fired even if the animation exits early")]
        public bool MustFire;
    }
    public class AnimationNotifier : StateMachineBehaviour
    {
        private int m_CompletedEventMask; //Limits number of events to 32, but that should be PLENTY
        private AnimAudioController m_Controller;

        [HideInInspector]
        public int NameHash; //Set from the relevant Editor
        public string AnimName;
        public AnimatorEvent[] AnimationEvents = new AnimatorEvent[0];

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_CompletedEventMask = 0;

            if (m_Controller == null)
                m_Controller = animator.GetComponent<AnimAudioController>();

            m_Controller.NotifyOfStateEnter(NameHash);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //If any MustFire events havent been fired but we are exiting the state, fire them now
            for (var i = 0; i < AnimationEvents.Length; i++)
            {
                if ((m_CompletedEventMask & (1 << i)) == 0 
                    && AnimationEvents[i].MustFire)
                {
                    m_CompletedEventMask |= (1 << i);
                    m_Controller.NotifyOfEvent(AnimationEvents[i].NameHash);
                }
            }

            m_Controller.NotifyOfStateExit(NameHash);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            for (var i = 0; i < AnimationEvents.Length; i++)
            {
                if ((m_CompletedEventMask & (1 << i)) == 0
                    && stateInfo.normalizedTime > AnimationEvents[i].EventTime)
                {
                    m_CompletedEventMask |= (1 << i);
                    m_Controller.NotifyOfEvent(AnimationEvents[i].NameHash);
                }
            }
        }
    }
}
