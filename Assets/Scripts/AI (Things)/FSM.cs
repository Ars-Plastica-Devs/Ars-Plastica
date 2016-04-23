using System;
using System.Collections.Generic;
using System.Linq;

public class FSM<TState>
{
    private float m_WaitToLeaveCounter { get; set; }
    private bool m_DoOnceCompleted { get; set; }

    private Transition<TState> m_EditingTransition { get; set; }
    private TState m_EditingState { get; set; }

    private Dictionary<TState, List<Transition<TState>>> m_Transitions { get; set; }
    private Dictionary<TState, List<Action>> m_EntryActions { get; set; }
    private Dictionary<TState, List<Action>> m_ExitActions { get; set; }
    private Dictionary<TState, List<Action>> m_WhileActions { get; set; }
    private Dictionary<TState, List<Action>> m_DoOnceActions { get; set; }
    private Dictionary<TState, float> m_WaitToLeaveAmounts { get; set; }
    public TState CurrentState { get; private set; }

    public FSM()
    {
        m_Transitions = new Dictionary<TState, List<Transition<TState>>>();
        m_EntryActions = new Dictionary<TState, List<Action>>();
        m_ExitActions = new Dictionary<TState, List<Action>>();
        m_WhileActions = new Dictionary<TState, List<Action>>();
        m_DoOnceActions = new Dictionary<TState, List<Action>>();
        m_WaitToLeaveAmounts = new Dictionary<TState, float>();
    }

    public FSM<TState> In(TState state)
    {
        m_EditingState = state;
        m_EditingTransition = CreateNewTransition(state);

        return this;
    }

    public FSM<TState> GoTo(TState next)
    {
        m_EditingTransition.Next = next;
        return this;
    }

    public FSM<TState> If(Func<bool> cond)
    {
        if (m_EditingTransition.Condition != null)
        {
            m_EditingTransition = CreateNewTransition(m_EditingState);
        }

        m_EditingTransition.Condition = cond;
        return this;
    }

    public FSM<TState> ExecuteOnEntry(params Action[] actions)
    {
        if (!m_EntryActions.ContainsKey(m_EditingState))
        {
            //Unlikely to use more than 2 actions, so lets try to save some memory.
            m_EntryActions[m_EditingState] = new List<Action>(2);
        }

        foreach (var action in actions)
        {
            m_EntryActions[m_EditingState].Add(action);
        }

        return this;
    }

    public FSM<TState> ExecuteOnExit(params Action[] actions)
    {
        if (!m_ExitActions.ContainsKey(m_EditingState))
        {
            //Unlikely to use more than 2 actions, so lets try to save some memory.
            m_ExitActions[m_EditingState] = new List<Action>(2);
        }

        foreach (var action in actions)
        {
            m_ExitActions[m_EditingState].Add(action);
        }

        return this;
    }

    public FSM<TState> ExecuteWhileIn(params Action[] actions)
    {
        if (!m_WhileActions.ContainsKey(m_EditingState))
        {
            //Unlikely to use more than 2 actions, so lets try to save some memory.
            m_WhileActions[m_EditingState] = new List<Action>(2);
        }

        foreach (var action in actions)
        {
            m_WhileActions[m_EditingState].Add(action);
        }

        return this;
    }

    public FSM<TState> DoOnce(params Action[] actions)
    {
        if (!m_DoOnceActions.ContainsKey(m_EditingState))
        {
            //Unlikely to use more than 2 actions, so lets try to save some memory.
            m_DoOnceActions[m_EditingState] = new List<Action>(2);
        }

        foreach (var action in actions)
        {
            m_DoOnceActions[m_EditingState].Add(action);
        }

        return this;
    }

    public FSM<TState> WaitToLeave(float t)
    {
        m_WaitToLeaveAmounts[CurrentState] = t;

        return this;
    }

    public void Initialize(TState state)
    {
        if (CurrentState != null)
        {
            DoExitStateActions(CurrentState);
        }

        CurrentState = state;
        m_WaitToLeaveCounter = 0f;
        m_DoOnceCompleted = false;

        DoEnterStateActions(CurrentState);
    }

    public virtual void Update(float dt)
    {
        var changed = false;

        if (m_DoOnceActions.ContainsKey(CurrentState))
        {
            if(!m_DoOnceCompleted)
                DoDoOnceActions(CurrentState);

            foreach (var t in m_Transitions[CurrentState].Where(t => t.Condition()))
            {
                DoTransition(t);
                return;
            }
            return;
        }

        if (!m_Transitions.ContainsKey(CurrentState))
            return;

        foreach (var transition in m_Transitions[CurrentState].Where(t => t.Condition()))
        {
            if (m_WaitToLeaveAmounts.ContainsKey(CurrentState))
            {
                m_WaitToLeaveCounter += dt;
                if (m_WaitToLeaveAmounts[CurrentState] > m_WaitToLeaveCounter)
                {
                    break;
                }
            }

            changed = true;
            DoTransition(transition);
            break;
        }

        if (!changed)
        {
            DoWhileInStateActions(CurrentState);
        }
    }

    private void DoTransition(Transition<TState> t)
    {
        DoExitStateActions(CurrentState);

        m_WaitToLeaveCounter = 0f;
        m_DoOnceCompleted = false;
        CurrentState = t.Next;

        DoEnterStateActions(CurrentState);
    }

    private void DoEnterStateActions(TState state)
    {
        List<Action> enterActions;
        if (m_EntryActions.TryGetValue(state, out enterActions))
        {
            foreach (var action in enterActions)
            {
                action();
            }
        }
    }

    private void DoExitStateActions(TState state)
    {
        List<Action> exitActions;
        if (m_ExitActions.TryGetValue(state, out exitActions))
        {
            foreach (var action in exitActions)
            {
                action();
            }
        }
    }

    private void DoWhileInStateActions(TState state)
    {
        List<Action> whileActions;
        if (m_WhileActions.TryGetValue(state, out whileActions))
        {
            foreach (var action in whileActions)
            {
                action();
            }
        }
    }

    private void DoDoOnceActions(TState state)
    {
        List<Action> doOnceAction;
        if (m_DoOnceActions.TryGetValue(state, out doOnceAction))
        {
            foreach (var action in doOnceAction)
            {
                action();
            }
        }
    }

    private Transition<TState> CreateNewTransition(TState from)
    {
        var trans = new Transition<TState>();

        if (!m_Transitions.ContainsKey(from))
        {
            m_Transitions[from] = new List<Transition<TState>>();
        }

        m_Transitions[from].Add(trans);

        return trans;
    }
}
