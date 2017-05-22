using UnityEngine;

public interface ICommandReceiver
{
    bool IsCommandRelevant(string cmd, GameObject sender = null);
    string RunCommand(string cmd, GameObject sender);
}