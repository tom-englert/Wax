namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using tomenglertde.Wax.Model.Wix;

    public class Solution
    {
        private readonly EnvDTE.Solution _solution;

        public Solution(EnvDTE.Solution solution)
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

        public string? FullName => _solution.FullName;

        public IEnumerable<Project> Projects { get; }

        public IEnumerable<WixProject> WixProjects { get; }

        public IEnumerable<Project> EnumerateTopLevelProjects => Projects
            .Where(project => project.IsTopLevelProject)
            .OrderBy(project => project.Name);
    }
}
