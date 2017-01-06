namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    public class ProjectReference : IEquatable<ProjectReference>
    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly VSLangProj.Reference _reference;

        public ProjectReference([NotNull] Solution solution, [NotNull] VSLangProj.Reference reference)
        {
            Contract.Requires(solution != null);
            Contract.Requires(reference != null);
            Contract.Requires(reference.SourceProject != null);

            _solution = solution;
            _reference = reference;
        }

        [NotNull]
        public Project SourceProject
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);

                var project = _solution.Projects.SingleOrDefault(p => string.Equals(p.UniqueName, _reference.SourceProject.UniqueName, StringComparison.OrdinalIgnoreCase));
                
                Contract.Assume(project != null);

                return project;
            }
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
            var identity = _reference.Identity;
            Contract.Assume(identity != null);
            return identity.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectReference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectReference"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectReference"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectReference"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectReference other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ProjectReference left, ProjectReference right)
        {
            if (object.ReferenceEquals(left, right))
                return true;
            if (object.ReferenceEquals(left, null))
                return false;
            if (object.ReferenceEquals(right, null))
                return false;

            return left._reference.Identity == right._reference.Identity;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ProjectReference left, ProjectReference right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectReference left, ProjectReference right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_reference != null);
        }
    }
}