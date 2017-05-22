using System;
using UnityEngine;

[Serializable]
public class ServerListing
{
    public ServerData[] Servers;

    public static ServerListing FromWebJSON(string address)
    {
        return new ServerListing
        {
            Servers = new[]
            {
                new ServerData("Any", "DH", "ps529225.dreamhostps.com"),
                new ServerData("Any", "localhost", "localhost")
            }
        };

        /*var www = new WWW(address);
        while (!www.isDone)
        {
        }
        return JsonUtility.FromJson<ServerListing>(www.text.Trim());*/
    }

    [Serializable]
    public class ServerData
    {
        public string Version;
        public string Name;
        public string IP;

        public ServerData(string version, string name, string ip)
        {
            Version = version;
            Name = name;
            IP = ip;
        }
    }
}