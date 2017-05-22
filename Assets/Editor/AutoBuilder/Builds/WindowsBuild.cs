using System.IO;
using UnityEditor;

namespace Assets.Editor.AutoBuilder.Builds
{
    public class WindowsBuild : AutomatedBuild
    {
        private readonly string[] m_Levels = { "Assets/Scenes/Menu.unity", "Assets/Scenes/arsplastica.unity" };
        private readonly string[] m_CopyDirectories = { "Data" };

        public override BuildTarget Target
        {
            get { return BuildTarget.StandaloneWindows; }
        }

        public override BuildOptions Options
        {
            get { return BuildOptions.None; }
        }

        public override string[] Levels
        {
            get { return m_Levels; }
        }

        public override string[] DirectoriesToCopyIntoBuild
        {
            get { return m_CopyDirectories; }
        }

        public override string DisplayName
        {
            get { return "Windows"; }
        }

        public override string SubFolder
        {
            get { return "Windows"; }
        }

        public override string GetExecutableName(string version)
        {
            return "ap_" + version;
        }

        public override void PostBuild(string version, string buildPath)
        {
            var debugFileOne = Path.Combine(buildPath, "player_win_x86.pdb");
            var debugFileTwo = Path.Combine(buildPath, "player_win_x86_s.pdb");

            if (File.Exists(debugFileOne))
            {
                File.Delete(debugFileOne);
            }
            if (File.Exists(debugFileTwo))
            {
                File.Delete(debugFileTwo);
            }
        }
    }
}
