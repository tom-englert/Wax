namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
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
            _solution = solution;
            _reference = reference;
        }

        [CanBeNull]
        public Project SourceProject => _solution.Projects.SingleOrDefault(p => string.Equals(p.UniqueName, _reference.SourceProject?.UniqueName, StringComparison.OrdinalIgnoreCase));

        [Equals, UsedImplicitly, CanBeNull]
        private string Identity => _reference.Identity;
    }
}