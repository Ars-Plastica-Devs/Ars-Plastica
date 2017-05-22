using UnityEditor;

namespace Assets.Editor.AutoBuilder
{
    public abstract class AutomatedBuild
    {
        public int Priority = 0;
        /// <summary>
        ///     The target platform for this build.
        /// </summary>
        public abstract BuildTarget Target { get; }
        /// <summary>
        ///     The options for this AutomatedBuild. Note that you can
        ///     use Bitwise operations to concantenate multiple options
        ///     ex: BuildOptions.ConnectWithProfiler | BuildOptions.AutoRunPlayer
        /// </summary>
        public abstract BuildOptions Options { get; }
        /// <summary>
        ///     The paths to any scenes to build with this AutomatedBuild,
        ///     relative to the project root folder.
        /// </summary>
        public abstract string[] Levels { get; }
        /// <summary>
        ///     Any extra directories you would like copied into the
        ///     build SubFolder with this build. Paths should be relative
        ///     to the project root folder.
        /// </summary>
        public abstract string[] DirectoriesToCopyIntoBuild { get; }
        /// <summary>
        ///     The name to display in the UI for this build option.
        /// </summary>
        public abstract string DisplayName { get; }
        /// <summary>
        ///     The folder to place this build in within the builds root folder.
        /// </summary>
        public abstract string SubFolder { get; }
        /// <summary>
        ///     The executable name for this build - with or without the extension
        /// </summary>
        public abstract string GetExecutableName(string version);

        public virtual void PreBuild(string version, string buildPath)
        {
        }

        public virtual void PostBuild(string version, string buildPath)
        {
        }
    }
}
