using System;
using System.Collections.Generic;

public class FSM<TState>
{
    private struct ExecuteForAction
    {
        public Action Action;
        public float Time;
    }

    private float m_TimeInStateCounter { get; set; }
    private float m_WaitToLeaveCounter { get; set; }
    private bool m_DoOnceCompleted { get; set; }

    private Transition<TState> m_EditingTransition { get; set; }
    private TState m_EditingState { get; set; }

    private Dictionary<TState, List<Transition<TState>>> m_Transitions { get; set; }
    private Dictionary<TState, List<Action>> m_EntryActions { get; set; }
    private Dictionary<TState, List<Action>> m_ExitActions { get; set; }
    private Dictionary<TState, List<Action>> m_WhileActions { get; set; }
    private Dictionary<TState, List<Action>> m_DoOnceActions { get; set; }
    private Dictionary<TState, List<ExecuteForAction>> m_ExecuteForActions { get; set; }
    private Dictionary<TState, float> m_WaitToLeaveAmounts { get; set; }
    public TState CurrentState { get; private set; }

    public FSM()
    {
        m_Transitions = new Dictionary<TState, List<Transition<TState>>>();
        m_EntryActions = new Dictionary<TState, List<Action>>();
        m_ExitActions = new Dictionary<TState, List<Action>>();
        m_WhileActions = new Dictionary<TState, List<Action>>();
        m_DoOnceActions = new Dictionary<TState, List<Action>>();
        m_ExecuteForActions = new Dictionary<TState, List<ExecuteForAction>>();
        m_WaitToLeaveAmounts = new Dictionary<TState, float>();
    }

    public FSM(IEqualityComparer<TState> keyComp)
    {
        m_Transitions = new Dictionary<TState, List<Transition<TState>>>(keyComp);
        m_EntryActions = new Dictionary<TState, List<Action>>(keyComp);
        m_ExitActions = new Dictionary<TState, List<Action>>(keyComp);
        m_WhileActions = new Dictionary<TState, List<Action>>(keyComp);
        m_DoOnceActions = new Dictionary<TState, List<Action>>(keyComp);
        m_ExecuteForActions = new Dictionary<TState, List<ExecuteForAction>>(keyComp);
        m_WaitToLeaveAmounts = new Dictionary<TState, float>(keyComp);
    }

    public FSM<TState> In(TState state)
    {
        m_EditingState = state;

        return this;
    }

    public FSM<TState> GoTo(TState next)
    {
        m_EditingTransition.Next = next;
        return this;
    }

    public FSM<TState> If(Func<bool> cond)
    {
        if (m_EditingTransition == null || m_EditingTransition.Condition != null)
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
            m_EntryActions[m_EditingState] = new List<Action>(actions.Length);
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
            m_ExitActions[m_EditingState] = new List<Action>(actions.Length);
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
            m_WhileActions[m_EditingState] = new List<Action>(actions.Length);
        }

        foreach (var action in actions)
        {
            m_WhileActions[m_EditingState].Add(action);
        }

        return this;
    }

    public FSM<TState> ExecuteFor(Action action, float time)
    {
        if (!m_ExecuteForActions.ContainsKey(m_EditingState))
        {
            m_ExecuteForActions[m_EditingState] = new List<ExecuteForAction>(1);
        }

        var forAction = new ExecuteForAction
        {
            Action = action,
            Time = time
        };

        m_ExecuteForActions[m_EditingState].Add(forAction);

        return this;
    }

    public FSM<TState> DoOnce(params Action[] actions)
    {
        if (!m_DoOnceActions.ContainsKey(m_EditingState))
        {
            m_DoOnceActions[m_EditingState] = new List<Action>(actions.Length);
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
        //var changed = false;

        m_TimeInStateCounter += dt;

        if (m_DoOnceActions.ContainsKey(CurrentState))
        {
            if(!m_DoOnceCompleted)
                DoDoOnceActions(CurrentState);

            //TODO: Can we remove this transition check, and instead use the one below?
            if (m_Transitions.ContainsKey(CurrentState))
            {
                for (var i = 0; i < m_Transitions[CurrentState].Count; i++)
                {
                    if (m_Transitions[CurrentState][i].Condition != null && m_Transitions[CurrentState][i].Condition())
                    {
                        DoTransition(m_Transitions[CurrentState][i]);
                        return;
                    }
                }

                /*foreach (var t in m_Transitions[CurrentState].Where(t => t.Condition != null && t.Condition()))
                {
                    DoTransition(t);
                    return;
                }*/
            }
            
            return;
        }

        if (m_Transitions.ContainsKey(CurrentState))
        {
            for (var i = 0; i < m_Transitions[CurrentState].Count; i++)
            {
                if (m_Transitions[CurrentState][i].Condition != null && m_Transitions[CurrentState][i].Condition())
                {
                    if (m_WaitToLeaveAmounts.ContainsKey(CurrentState))
                    {
                        m_WaitToLeaveCounter += dt;
                        if (m_WaitToLeaveAmounts[CurrentState] > m_WaitToLeaveCounter)
                        {
                            break;
                        }
                    }

                    DoTransition(m_Transitions[CurrentState][i]);
                    return;
                }
            }

            /*foreach (var transition in m_Transitions[CurrentState].Where(t => t.Condition != null && t.Condition()))
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
            }*/
        }

        DoWhileInStateActions(CurrentState);
        DoExecuteForActions(CurrentState);
    }

    private void DoTransition(Transition<TState> t)
    {
        DoExitStateActions(CurrentState);

        m_WaitToLeaveCounter = 0f;
        m_TimeInStateCounter = 0f;
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

    private void DoExecuteForActions(TState state)
    {
        List<ExecuteForAction> forActions;
        if (m_ExecuteForActions.TryGetValue(state, out forActions))
        {
            for (var i = 0; i < forActions.Count; i++)
            {
                if (forActions[i].Time < m_TimeInStateCounter)
                    forActions[i].Action();
            }
        }
    }

    private void DoDoOnceActions(TState state)
    {
        List<Action> doOnceAction;
        if (m_DoOnceActions.TryGetValue(state, out doOnceAction))
        {
            doOnceAction.ForEach(a => a());
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
            m_Transitions[from] = new List<Transition<TState>>(2);
        }

        m_Transitions[from].Add(trans);

        return trans;
    }
}
