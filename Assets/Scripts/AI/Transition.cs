using System;

internal class Transition<TState>
{
    private Func<bool> m_Condition; 
    public TState Next { get; set; }
    public Func<bool> Condition
    {
        get
        {
            return m_Condition ?? (() => false);
        }
        set { m_Condition = value; }
    }
}
