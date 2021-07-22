namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.IO;

    public class ProjectOutput
    {
        public ProjectOutput(Project project, string relativeFileName, BuildFileGroups buildFileGroup, string binaryTargetDirectory)
        {
            Project = project;

            var prefix = binaryTargetDirectory + @"\";

            if ((buildFileGroup != BuildFileGroups.ContentFiles) && relativeFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                SourceName = relativeFileName.Substring(prefix.Length);
            }
            else
            {
                SourceName = relativeFileName;
            }

            BuildFileGroup = buildFileGroup;

            TargetName = (BuildFileGroup == BuildFileGroups.ContentFiles) ? SourceName : Path.Combine(binaryTargetDirectory, SourceName);
        }

        public ProjectOutput(Project project, string relativeFileName, string binaryTargetDirectory)
        {
            Project = project;
            SourceName = relativeFileName;
            TargetName = Path.Combine(binaryTargetDirectory, relativeFileName);
        }

        public ProjectOutput(Project project, VSLangProj.Reference reference, string binaryTargetDirectory)
            : this(project, Path.GetFileName(reference.Path), binaryTargetDirectory)
        {
        }

        public string SourceName { get; }

        public string TargetName { get; }

        public Project Project { get; }

        public bool IsReference => BuildFileGroup == BuildFileGroups.None;

        public BuildFileGroups BuildFileGroup { get; }

        public override string ToString()
        {
            return TargetName;
        }
    }
}
