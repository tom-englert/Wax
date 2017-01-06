namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    public class ProjectOutput : IEquatable<ProjectOutput>
    {
        [NotNull]
        private readonly string _fullName;

        [NotNull]
        private readonly Project _project;

        public ProjectOutput([NotNull] Project project, [NotNull] string fullName, BuildFileGroups buildFileGroup)
        {
            Contract.Requires(project != null);
            Contract.Requires(fullName != null);

            BuildFileGroup = buildFileGroup;
            _fullName = fullName;
            _project = project;
        }

        public ProjectOutput([NotNull] Project project, [NotNull] string fullName, [NotNull] string targetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(fullName != null);
            Contract.Requires(targetDirectory != null);

            _fullName = Path.Combine(targetDirectory, Path.GetFileName(fullName));
            _project = project;
        }

        [ContractVerification(false), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public ProjectOutput([NotNull] Project project, [NotNull] VSLangProj.Reference reference, [NotNull] string targetDirectory)
            : this(project, reference.Path, targetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(reference != null);
            Contract.Requires(targetDirectory != null);
        }

        [NotNull]
        public string SourceName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                switch (BuildFileGroup)
                {
                    case BuildFileGroups.Built:
                    case BuildFileGroups.Symbols:
                    case BuildFileGroups.None: // references...
                        return FileName;

                    default:
                        return _fullName;
                }
            }
        }


        [NotNull]
        public string TargetName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _fullName;
            }
        }

        [NotNull]
        public string FileName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetFileName(_fullName);
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
            return _fullName;
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _fullName.ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectOutput);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectOutput"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectOutput"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectOutput"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectOutput other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ProjectOutput left, ProjectOutput right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return string.Equals(left._fullName, right._fullName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ProjectOutput left, ProjectOutput right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectOutput left, ProjectOutput right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_fullName != null);
            Contract.Invariant(_project != null);
        }

    }
}
