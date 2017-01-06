namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Wix;

    public class Solution
    {
        [NotNull]
        private readonly EnvDTE.Solution _solution;
        [NotNull, ItemNotNull]
        private readonly IEnumerable<Project> _projects;
        [NotNull, ItemNotNull]
        private readonly IEnumerable<WixProject> _wixProjects;

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "No contracts for VS objects")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "No contracts for VS objects")]
        public Solution([NotNull] EnvDTE.Solution solution)
        {
            Contract.Requires(solution != null);

            _solution = solution;

            _projects = _solution.GetProjects()
                .Select(project => new Project(this, project))
                .Where(project => project.IsVsProject)
                .OrderBy(project => project.Name)
                .ToArray();

            foreach (var project in _projects)
            {
                Contract.Assume(project != null);

                foreach (var dependency in project.GetProjectReferences())
                {
                    Contract.Assume(dependency != null);
                    dependency.SourceProject.ReferencedBy.Add(project);
                }
            }

            // Microsoft.Tools.WindowsInstallerXml.VisualStudio.OAWixProject
            _wixProjects = _solution.GetProjects()
                .Where(project => project.Kind.Equals("{930c7802-8a8c-48f9-8165-68863bccd9dd}", StringComparison.OrdinalIgnoreCase))
                .Select(project => new WixProject(this, project))
                .OrderBy(project => project.Name)
                .ToArray();
        }

        public string FullName => _solution.FullName;

        [NotNull, ItemNotNull]
        public IEnumerable<Project> Projects
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

                return _projects;
            }
        }

        [NotNull, ItemNotNull]
        public IEnumerable<Project> TopLevelProjects
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

                return Projects
                    .Where(project => !project.IsTestProject)
                    .Where(project => project.ReferencedBy.All(reference => reference.IsTestProject))
                    .OrderBy(project => project.Name);
            }
        }

        [NotNull, ItemNotNull]
        public IEnumerable<WixProject> WixProjects
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixProject>>() != null);

                return _wixProjects;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_projects != null);
            Contract.Invariant(_wixProjects != null);
        }
    }
}
