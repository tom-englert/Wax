namespace tomenglertde.Wax.Model.VisualStudio
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ProjectOutputGroup
    {
        public ProjectOutputGroup(string targetName, IReadOnlyCollection<ProjectOutput> projectOutputs)
        {
            TargetName = targetName;
            ProjectOutputs = projectOutputs;
            Projects = new HashSet<Project>(projectOutputs.Select(p => p.Project));
        }

        public IReadOnlyCollection<ProjectOutput> ProjectOutputs { get; }

        public ICollection<Project> Projects { get; }

        public string TargetName { get; }

        public string FileName => Path.GetFileName(TargetName);

        public string SourceName => ProjectOutputs.First().SourceName;

        public override string ToString()
        {
            return TargetName;
        }
    }
}
