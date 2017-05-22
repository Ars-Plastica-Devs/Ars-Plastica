using System.IO;
using System.Text;
using UnityEditor;

namespace Assets.Editor.AutoBuilder.Builds
{
    public class LinuxWebGLServerBuild : AutomatedBuild
    {
        private readonly string[] m_Levels = { "Assets/Scenes/Menu-WebGL.unity", "Assets/Scenes/arsplastica-WebGL.unity" };
        private readonly string[] m_CopyDirectories = {"Data"};

        public LinuxWebGLServerBuild()
        {
            Priority = 1;
        }

        public override BuildTarget Target
        {
            get { return BuildTarget.StandaloneLinux64; }
        }

        public override BuildOptions Options
        {
            get { return BuildOptions.EnableHeadlessMode; }
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
            get { return "Linux WebGL Server"; }
        }

        public override string SubFolder
        {
            get { return "Server"; }
        }

        public override string GetExecutableName(string version)
        {
            return "ap_" + version + "_server";
        }

        public override void PreBuild(string version, string buildPath)
        {
            base.PreBuild(version, buildPath);
            SetIsServerAndVersion(true, version);
        }

        public override void PostBuild(string version, string buildPath)
        {
            base.PostBuild(version, buildPath);
            SetIsServerAndVersion(false, version);

            WriteServerRestartFile(version, buildPath);
        }

        private void SetIsServerAndVersion(bool val, string version)
        {
            using (var sceneEdit = new SceneEditHelper("Assets/Scenes/Menu-WebGL.unity"))
            {
                var arsNm = sceneEdit.GetFirstComponentOfType<ArsNetworkManager>();
                if (arsNm == null) return;

                Undo.RecordObject(arsNm, "IsServer and Version"); //this might not be needed, but it works so whatever
                arsNm.IsServer = val;
                arsNm.CurrentVersionNumber = version;
                EditorUtility.SetDirty(arsNm); //This is supposed to be deprecated, but its the only thing that works
            }
        }

        private void WriteServerRestartFile(string version, string buildPath)
        {
            var file = new FileInfo(Path.Combine(buildPath, "ServerRestart.py"));
            
            var sr = new StringBuilder();
            sr.AppendLine("import os");
            sr.AppendLine("import sys");
            sr.AppendLine("import signal");
            sr.AppendLine("import subprocess");
            sr.AppendLine("from subprocess import CalledProcessError, check_output");
            sr.AppendLine("processName = \"ap_" + version + "_server\"");
            sr.AppendLine("if len(sys.argv) >= 2:");
            sr.AppendLine("\tprocessName = \"ap_\" + sys.argv[1] + \"_server\"");
            sr.AppendLine("def get_pids(name):");
            sr.AppendLine("\ttry:");
            sr.AppendLine("\t\tpidList = map(int, check_output([\"pidof\", name + \".x86_64\"]).split())");
            sr.AppendLine("\texcept CalledProcessError:");
            sr.AppendLine("\t\tpidList = []");
            sr.AppendLine("\treturn pidList");
            sr.AppendLine("pids = get_pids(processName)");
            sr.AppendLine("for p in pids:");
            sr.AppendLine("\tos.kill(p, signal.SIGTERM)");
            sr.AppendLine("if len(sys.argv) == 3 and sys.argv[2] == \"kill\":");
            sr.AppendLine("\tsys.exit()");
            sr.AppendLine("subprocess.Popen(\"chmod \" + \"+x \" + processName + \".x86_64\", shell=True)");
            sr.AppendLine("subprocess.Popen(\"./\" + processName + \".x86_64 & disown\", shell=True)");

            File.WriteAllText(file.FullName, sr.ToString());
        }
    }
}
