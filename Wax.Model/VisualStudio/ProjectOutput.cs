namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.IO;

    using JetBrains.Annotations;

    public class ProjectOutput
    {
        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, BuildFileGroups buildFileGroup, [NotNull] string binaryTargetDirectory)
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

        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, [NotNull] string binaryTargetDirectory)
        {
            Project = project;
            SourceName = relativeFileName;
            TargetName = Path.Combine(binaryTargetDirectory, relativeFileName);
        }

        public ProjectOutput([NotNull] Project project, [NotNull] VSLangProj.Reference reference, [NotNull] string binaryTargetDirectory)
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(project, Path.GetFileName(reference.Path), binaryTargetDirectory)
        {
        }

        [NotNull]
        public string SourceName { get; }

        [NotNull]
        public string TargetName { get; }

        [NotNull]
        public Project Project { get; }

        public bool IsReference => BuildFileGroup == BuildFileGroups.None;

        public BuildFileGroups BuildFileGroup { get; }

        public override string ToString()
        {
            return TargetName;
        }
    }
}
