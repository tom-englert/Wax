namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Wix;

    public class Solution
    {
        [NotNull]
        private readonly EnvDTE.Solution _solution;

        public Solution([NotNull] EnvDTE.Solution solution)
        {
            _solution = solution;

            Projects = _solution.GetProjects()
                .Select(project => new Project(this, project))
                .Where(project => project.IsVsProject)
                .OrderBy(project => project.Name)
                .ToList().AsReadOnly();

            foreach (var project in Projects)
            {
                if (project == null)
                    continue;

                foreach (var dependency in project.GetProjectReferences())
                {
                    Debug.Assert(dependency != null);
                    dependency.SourceProject?.ReferencedBy.Add(project);
                }
            }

            // Microsoft.Tools.WindowsInstallerXml.VisualStudio.OAWixProject
            WixProjects = _solution.GetProjects()
                .Where(project => "{930c7802-8a8c-48f9-8165-68863bccd9dd}".Equals(project.Kind, StringComparison.OrdinalIgnoreCase))
                .Select(project => new WixProject(this, project))
                .OrderBy(project => project.Name)
                .ToList().AsReadOnly();
        }

        [CanBeNull]
        public string FullName => _solution.FullName;

        [NotNull, ItemNotNull]
        public IEnumerable<Project> Projects { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<WixProject> WixProjects { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<Project> EnumerateTopLevelProjects => Projects
            .Where(project => project.IsTopLevelProject)
            .OrderBy(project => project.Name);
    }
}
