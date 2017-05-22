using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class AnimAudioController : NetworkBehaviour
{
    protected AudioSource AudioSource;
    protected Animator Anim;

    protected readonly Dictionary<int, Action> OnEnterHandlers = new Dictionary<int, Action>();
    protected readonly Dictionary<int, Action> OnExitHandlers = new Dictionary<int, Action>();
    protected readonly Dictionary<int, Action> OnEventHandlers = new Dictionary<int, Action>();

    public delegate void AnimationEventDelegate();

    protected virtual void Awake()
    {
        Anim = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
    }

    public void NotifyOfStateEnter(int stateHash)
    {
        Action del;
        if (OnEnterHandlers.TryGetValue(stateHash, out del))
        {
            if (del != null) del();
        }
    }

    public void NotifyOfStateExit(int stateHash)
    {
        Action del;
        if (OnExitHandlers.TryGetValue(stateHash, out del))
        {
            if (del != null) del();
        }
    }

    public void NotifyOfEvent(int eventNameHash)
    {
        Action del;
        if (OnEventHandlers.TryGetValue(eventNameHash, out del))
        {
            if (del != null) del();
        }
    }

    protected void AddEnterHandler(string animName, Action action)
    {
        OnEnterHandlers.Add(Animator.StringToHash(animName), action);
    }

    protected void AddEventHandler(string animName, Action action)
    {
        OnEventHandlers.Add(Animator.StringToHash(animName), action);
    }

    protected void AddExitHandler(string animName, Action action)
    {
        OnExitHandlers.Add(Animator.StringToHash(animName), action);
    }
}
