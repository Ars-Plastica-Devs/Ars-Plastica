using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class DataStore
{
    private static readonly FileInfo DataFile;
    private static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
    private static readonly Dictionary<string, int> DataLocations = new Dictionary<string, int>(); 

    public const string DataPath = "Data/data.txt";

    static DataStore()
    {
        DataFile = new FileInfo(DataPath);

        if (!DataFile.Exists) return;

        LoadData();
    }

    public static string Get(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? Data[key]
                : null;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => line.Substring(key.Length + 1))
            .FirstOrDefault();
    }

    public static int GetInt(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? int.Parse(Data[key])
                : 0;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => int.Parse(line.Substring(key.Length + 1)))
            .FirstOrDefault();
    }

    public static float GetFloat(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? float.Parse(Data[key])
                : 0;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => float.Parse(line.Substring(key.Length + 1)))
            .FirstOrDefault();
    }

    public static void Set(string key, string value)
    {
        if (!Application.isPlaying)
        {
            SetEditor(key, value);
            return;
        }

        Data[key] = value;

        var arrLine = File.ReadAllLines(DataPath);
        arrLine[DataLocations[key]] = key + ' ' + value;
        File.WriteAllLines(DataPath, arrLine);
    }

    public static void Set(string key, object o)
    {
        Set(key, o.ToString());
    }

    public static void SetEditor(string key, string value)
    {
        var arrLine = File.ReadAllLines(DataPath);

        for (var i = 0; i < arrLine.Length; i++)
        {
            if (!arrLine[i].StartsWith(key + ' ')) continue;

            arrLine[i] = key + ' ' + value;
            break;
        }

        File.WriteAllLines(DataPath, arrLine);
    }

    private static void LoadData()
    {
        using (var sr = new StreamReader(DataFile.FullName))
        {
            string line;
            var lineNumber = 0;

            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line) || line[0] == '/')
                {
                    lineNumber++;
                    continue;
                }

                var firstSpace = line.IndexOf(' ');
                var key = line.Substring(0, firstSpace);
                var value = line.Substring(firstSpace + 1, line.Length - (key.Length + 1));

                Data.Add(key, value);
                DataLocations.Add(key, lineNumber);

                lineNumber++;
            }
        }
    }
}
