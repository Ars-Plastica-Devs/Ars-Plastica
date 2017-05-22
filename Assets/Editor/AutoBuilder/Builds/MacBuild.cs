using UnityEditor;

namespace Assets.Editor.AutoBuilder.Builds
{
    public class MacBuild : AutomatedBuild
    {
        private readonly string[] m_Levels = { "Assets/Scenes/Menu.unity", "Assets/Scenes/arsplastica.unity" };
        private readonly string[] m_CopyDirectories = { "Data" };

        public override BuildTarget Target
        {
            get { return BuildTarget.StandaloneOSXIntel; }
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
            get { return "Mac"; }
        }

        public override string SubFolder
        {
            get { return "Mac"; }
        }

        public override string GetExecutableName(string version)
        {
            return "ap_" + version;
        }
    }
}
