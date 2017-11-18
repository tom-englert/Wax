namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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
        [NotNull]
        private readonly Project _project;

        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, BuildFileGroups buildFileGroup, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(relativeFileName != null);
            Contract.Requires(binaryTargetDirectory != null);

            _project = project;

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
            Contract.Requires(project != null);
            Contract.Requires(relativeFileName != null);
            Contract.Requires(binaryTargetDirectory != null);

            _project = project;
            _relativeFileName = relativeFileName;
            _targetName = Path.Combine(binaryTargetDirectory, relativeFileName);
        }

        [ContractVerification(false), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public ProjectOutput([NotNull] Project project, [NotNull] VSLangProj.Reference reference, [NotNull] string binaryTargetDirectory)
            : this(project, Path.GetFileName(reference.Path), binaryTargetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(reference != null);
            Contract.Requires(binaryTargetDirectory != null);
        }

        [NotNull]
        public string SourceName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _relativeFileName;
            }
        }

        [NotNull]
        public string TargetName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _targetName;
            }
        }

        [NotNull]
        public string FileName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetFileName(_relativeFileName);
            }
        }

        [NotNull]
        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);

                return _project;
            }
        }

        public bool IsReference => BuildFileGroup == BuildFileGroups.None;

        public BuildFileGroups BuildFileGroup { get; }

        public override string ToString()
        {
            return _targetName;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_relativeFileName != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_targetName != null);
        }

    }
}
