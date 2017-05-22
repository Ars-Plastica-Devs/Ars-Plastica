using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

public static class AutoBuildCompressor
{
    public static void Compress(string folder, string baseDirectory, string archiveName)
    {
        if (!Directory.Exists(folder))
        {
            Debug.Log("Cannot find directory " + folder);
            return;
        }

        var unityFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
        if (unityFolder == null)
        {
            Debug.Log("Error while trying to find the Unity application folder.");
            return;
        }

        var pathTo7Z = Path.Combine(unityFolder, "Data/Tools/7z");
        if (!(new DirectoryInfo(Path.Combine(unityFolder, "Data/Tools/")).GetFiles("7z.*").Length > 0))
        {
            Debug.Log("Cannot find path to 7z, checking: " + pathTo7Z);
            return;
        }

        //Format the folder name for 7Zips args
        if (!folder.EndsWith("/"))
            folder += "/*";
        if (!folder.EndsWith("*"))
            folder += "*";

        var args = "a -tZip " + archiveName + ".zip " + folder;

        var processInfo = new ProcessStartInfo(pathTo7Z, args)
        {
            WorkingDirectory = baseDirectory, //Removes useless directories from the archive path
            UseShellExecute = false
        };

        Process.Start(processInfo);
    }
}
