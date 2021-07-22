namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Linq;

    using Equatable;

    [ImplementsEquatable]
    public class ProjectReference
    {
        private readonly Solution _solution;
        private readonly VSLangProj.Reference _reference;

        public ProjectReference(Solution solution, VSLangProj.Reference reference)
        {
            _solution = solution;
            _reference = reference;
        }

        public Project? SourceProject => _solution.Projects.SingleOrDefault(p => string.Equals(p.UniqueName, _reference.SourceProject?.UniqueName, StringComparison.OrdinalIgnoreCase));

        [Equals]
        public string? Identity => _reference.Identity;
    }
}