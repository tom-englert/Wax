namespace tomenglertde.Wax.Model.VisualStudio
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    public class ProjectOutputGroup
    {
        public ProjectOutputGroup([NotNull] string targetName, [NotNull, ItemNotNull] IReadOnlyCollection<ProjectOutput> projectOutputs)
        {
            TargetName = targetName;
            ProjectOutputs = projectOutputs;
            Projects = new HashSet<Project>(projectOutputs.Select(p => p.Project));
        }

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ProjectOutput> ProjectOutputs { get; }

        [NotNull, ItemNotNull]
        public ICollection<Project> Projects { get; }

        [NotNull]
        public string TargetName { get; }

        [NotNull]
        public string FileName => Path.GetFileName(TargetName);

        [NotNull]
        public string SourceName => ProjectOutputs.First().SourceName;

        public override string ToString()
        {
            return TargetName;
        }
    }
}
