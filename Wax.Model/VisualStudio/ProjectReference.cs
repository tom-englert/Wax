namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public class ProjectReference
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

        [CanBeNull]
        public Project SourceProject
        {
            get
            {
                var project = _solution.Projects.SingleOrDefault(p => string.Equals(p.UniqueName, _reference.SourceProject?.UniqueName, StringComparison.OrdinalIgnoreCase));

                return project;
            }
        }

        [Equals, UsedImplicitly, CanBeNull]
        private string Identity => _reference.Identity;


        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_reference != null);
        }
    }
}