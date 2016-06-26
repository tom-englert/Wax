namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using tomenglertde.Wax.Model.Wix;

    using VSLangProj;

    public class Solution
    {
        private readonly EnvDTE.Solution _solution;
        private readonly IEnumerable<Project> _projects;
        private readonly IEnumerable<WixProject> _wixProjects;

        public Solution(EnvDTE.Solution solution)
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
                foreach (var dependency in project.ProjectReferences)
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

        public string FullName
        {
            get
            {
                return _solution.FullName;
            }
        }

        public IEnumerable<Project> Projects
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Project>>() != null);

                return _projects;
            }
        }

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

        public IEnumerable<WixProject> WixProjects
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixProject>>() != null);

                return _wixProjects;
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_projects != null);
            Contract.Invariant(_wixProjects != null);
        }
    }
}
