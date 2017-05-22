using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.AutoBuilder
{
    public class AutoBuildWindow : EditorWindow
    {
        //These constants are used for EditorPrefs keys
        private const string VERSION_NUMBER_KEY = "AutoBuildVersionNumber";
        private const string BUILD_ROOT_PATH_KEY = "AutoBuildBuildRootPath";
        private const string BUILD_NAME_KEY = "AutoBuildBuildName";
        private const string COMPRESSED_FOLDER_KEY = "AutoBuildCompressedFolder";
        private const string OUTPUT_COMPRESSED_FOLDER_KEY = "AutoBuildOutputCompressedFolder";

        private readonly List<AutomatedBuild> m_Builds = new List<AutomatedBuild>();
        private readonly Dictionary<AutomatedBuild, bool> m_BuildActiveState = new Dictionary<AutomatedBuild, bool>();

        private string m_VersionNumber;
        private string m_BuildRootFolderPath;
        private string m_BuildName;

        private bool m_OutputCompressedFolder;
        private string m_CompressedFolderName;

        private string m_BuildFolderName
        {
            get { return m_BuildName + m_VersionNumber; }
        }

        [MenuItem("Auto-Build/Build New Version %&b")]
        public static void BuildNewVersion()
        {
            var window = GetWindow<AutoBuildWindow>("Auto-Build");
            window.Show();
        }

        public void OnEnable()
        {
            var buildTypes = ReflectionHelper.GetConcreteDescendantTypes<AutomatedBuild>();

            var buildOptions = buildTypes.Select(t => (AutomatedBuild) Activator.CreateInstance(t)).ToList();
            buildOptions.Sort((a,b) => -a.Priority.CompareTo(b.Priority));
            foreach (var t in buildOptions)
            {
                m_Builds.Add(t);
                m_BuildActiveState.Add(t, true);
            }

            m_VersionNumber = EditorPrefs.GetString(VERSION_NUMBER_KEY, "");
            m_BuildRootFolderPath = EditorPrefs.GetString(BUILD_ROOT_PATH_KEY, "Builds");
            m_BuildName = EditorPrefs.GetString(BUILD_NAME_KEY, "");
            m_CompressedFolderName = EditorPrefs.GetString(COMPRESSED_FOLDER_KEY, "");
            m_OutputCompressedFolder = EditorPrefs.GetBool(OUTPUT_COMPRESSED_FOLDER_KEY, false);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build Name", EditorStyles.boldLabel);
            m_BuildName = EditorGUILayout.TextField(m_BuildName, GUILayout.Width(200f));

            EditorGUILayout.LabelField("Version Number", EditorStyles.boldLabel);
            m_VersionNumber = EditorGUILayout.TextField(m_VersionNumber, GUILayout.Width(200f));

            EditorGUILayout.LabelField("Builds Root Folder", EditorStyles.boldLabel);
            m_BuildRootFolderPath = EditorGUILayout.TextField(m_BuildRootFolderPath, GUILayout.Width(200f));

            var buildFolder = Path.Combine(m_BuildRootFolderPath, m_BuildFolderName);
            foreach (var build in m_Builds)
            {
                var prospectivePath = Path.Combine(buildFolder, build.SubFolder);
                prospectivePath = Path.Combine(prospectivePath, build.GetExecutableName(m_VersionNumber));
                m_BuildActiveState[build] = GUILayout.Toggle(m_BuildActiveState[build], build.DisplayName + " (" + prospectivePath + ")");
            }

            GUILayout.Space(10f);

            m_OutputCompressedFolder = GUILayout.Toggle(m_OutputCompressedFolder, "Output Additional Compressed Folder");

            if (m_OutputCompressedFolder)
            {
                EditorGUILayout.LabelField("Compressed Folder Name", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(buildFolder + (buildFolder.EndsWith("\\") ? "" : "\\"));
                m_CompressedFolderName = EditorGUILayout.TextField(m_CompressedFolderName, GUILayout.Width(125f));
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Build", GUILayout.Width(50f)))
            {
                if (string.IsNullOrEmpty(m_VersionNumber))
                    Debug.Log("Cannot build without a version number!");
                else
                {
                    Build(m_VersionNumber);

                    if (m_OutputCompressedFolder)
                    {
                        if (string.IsNullOrEmpty(m_CompressedFolderName))
                            Debug.Log("Cannot compress without a folder name");
                        else
                        {
                            var baseDir = Path.Combine(m_BuildRootFolderPath, m_BuildFolderName);
                            AutoBuildCompressor.Compress(Path.GetFullPath(buildFolder), baseDir, m_CompressedFolderName);
                        }
                    }
                }
            }
        }

        private void Build(string version)
        {
            foreach (var build in m_Builds.Where(b => m_BuildActiveState[b]))
            {
                RunAutomatedBuild(build, version, Path.Combine(m_BuildRootFolderPath, m_BuildFolderName));
            }
        }

        private void RunAutomatedBuild(AutomatedBuild build, string version, string path)
        {
            var buildSubFolder = Path.Combine(path, build.SubFolder);

            var extension = string.Empty;
            try
            {
                extension = GetExtension(build.Target);
            }
            catch (ArgumentException e)
            {
                Debug.Log(e);
                return;
            }

            var exeName = build.GetExecutableName(version);

            //Strip a trailing period
            if (exeName.EndsWith("."))
                exeName = exeName.Substring(0, exeName.Length - 1);

            //TODO: We might not know the extension - should handle that case and accept the user-given extension
            //Add the extension if it was not provided by the user
            if (!exeName.EndsWith(extension))
                exeName += extension;

            build.PreBuild(version, buildSubFolder);

            BuildPipeline.BuildPlayer(build.Levels,
                Path.Combine(buildSubFolder, exeName), build.Target, build.Options);

            build.PostBuild(version, buildSubFolder);

            foreach (var copyDir in build.DirectoriesToCopyIntoBuild.Where(s => !string.IsNullOrEmpty(s)))
            {
                DirectoryCopy(copyDir, Path.Combine(buildSubFolder, copyDir));
            }
        }

        private void DirectoryCopy(string source, string dest)
        {
            var sourceDir = new DirectoryInfo(source);

            if (!sourceDir.Exists)
            {
                Debug.Log("Cannot find " + source + " directory to copy to Build folder");
                return;
            }

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            var files = sourceDir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(dest, file.Name);
                file.CopyTo(tempPath, true);
            }

            foreach (var subdir in sourceDir.GetDirectories())
            {
                var temppath = Path.Combine(dest, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }

        private string GetExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSXUniversal:
                    return ".app";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneLinux64:
                    return ".x86_64";
                case BuildTarget.StandaloneLinux:
                    return ".x86";
                case BuildTarget.Android:
                    return ".apk";
                default:
                    return ""; 
                    //throw new ArgumentException("Cannot determine the extension for " + target + ". Return the extension manually within your overridden AutomatedBuild.GetExecutableName.");
            }
        }

        private void OnDestroy()
        {
            EditorPrefs.SetString(VERSION_NUMBER_KEY, m_VersionNumber);
            EditorPrefs.SetString(BUILD_ROOT_PATH_KEY, m_BuildRootFolderPath);
            EditorPrefs.SetString(BUILD_NAME_KEY, m_BuildName);
            EditorPrefs.SetString(COMPRESSED_FOLDER_KEY, m_CompressedFolderName);
            EditorPrefs.SetBool(OUTPUT_COMPRESSED_FOLDER_KEY, m_OutputCompressedFolder);
        }
    }
}
