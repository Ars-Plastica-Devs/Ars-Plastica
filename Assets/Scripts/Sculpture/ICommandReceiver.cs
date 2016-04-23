using UnityEngine;

public interface ICommandReceiver
{
    bool IsCommandRelevant(string cmd);
    string RunCommand(string cmd, GameObject sender);
}