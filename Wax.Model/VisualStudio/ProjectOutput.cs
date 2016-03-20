namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;

    public class ProjectOutput : IEquatable<ProjectOutput>
    {
        private readonly string _fullName;
        private readonly BuildFileGroups _buildFileGroup;
        private readonly Project _project;
        private readonly VSLangProj.Reference _reference;

        public ProjectOutput(Project project, string fullName, BuildFileGroups buildFileGroup)
        {
            Contract.Requires(project != null);
            Contract.Requires(fullName != null);

            _fullName = fullName;
            _buildFileGroup = buildFileGroup;
            _project = project;
        }

        public ProjectOutput(Project project, VSLangProj.Reference reference)
        {
            Contract.Requires(project != null);
            Contract.Requires(reference != null);

            _reference = reference;
            _fullName = reference.Path;
            Contract.Assume(_fullName != null);
            _project = project;
        }

        public string FullName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _fullName;
            }
        }

        public string FileName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetFileName(_fullName);
            }
        }

        public BuildFileGroups BuildFileGroup
        {
            get
            {
                return _buildFileGroup;
            }
        }

        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);

                return _project;
            }
        }

        public bool IsReference
        {
            get
            {
                return _reference != null;
            }
        }

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
        private void ObjectInvariant()
        {
            Contract.Invariant(_fullName != null);
            Contract.Invariant(_project != null);
        }

    }
}
