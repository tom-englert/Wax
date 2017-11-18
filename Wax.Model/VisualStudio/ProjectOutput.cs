namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.IO;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public class ProjectOutput
    {
        [NotNull]
        private readonly string _relativeFileName;
        [Equals(StringComparison.OrdinalIgnoreCase)]
        [NotNull]
        private readonly string _targetName;

        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, BuildFileGroups buildFileGroup, [NotNull] string binaryTargetDirectory)
        {
            Project = project;

            var prefix = binaryTargetDirectory + @"\";

            if ((buildFileGroup != BuildFileGroups.ContentFiles) && relativeFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _relativeFileName = relativeFileName.Substring(prefix.Length);
            }
            else
            {
                _relativeFileName = relativeFileName;
            }

            BuildFileGroup = buildFileGroup;

            _targetName = (BuildFileGroup == BuildFileGroups.ContentFiles) ? _relativeFileName : Path.Combine(binaryTargetDirectory, _relativeFileName);
        }

        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, [NotNull] string binaryTargetDirectory)
        {
            Project = project;
            _relativeFileName = relativeFileName;
            _targetName = Path.Combine(binaryTargetDirectory, relativeFileName);
        }

        public ProjectOutput([NotNull] Project project, [NotNull] VSLangProj.Reference reference, [NotNull] string binaryTargetDirectory)
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(project, Path.GetFileName(reference.Path), binaryTargetDirectory)
        {
        }

        [NotNull]
        public string SourceName => _relativeFileName;

        [NotNull]
        public string TargetName => _targetName;

        [NotNull]
        public string FileName => Path.GetFileName(_relativeFileName);

        [NotNull]
        public Project Project { get; }

        public bool IsReference => BuildFileGroup == BuildFileGroups.None;

        public BuildFileGroups BuildFileGroup { get; }

        public override string ToString()
        {
            return _targetName;
        }
    }
}
