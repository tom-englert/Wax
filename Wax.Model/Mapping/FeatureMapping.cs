namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    public class FeatureMapping
    {
        public FeatureMapping([NotNull] WixFeatureNode featureNode, [NotNull] ICollection<FileMapping> installedFiles,
            [NotNull] ICollection<Project> projects, [NotNull] ICollection<ProjectOutputGroup> requiredProjectOutputs, [NotNull] ICollection<ProjectOutputGroup> missingProjectOutputs)
        {
            FeatureNode = featureNode;
            InstalledFiles = installedFiles;
            Projects = projects;
            RequiredProjectOutputs = requiredProjectOutputs;
            MissingProjectOutputs = missingProjectOutputs;
        }

        [NotNull]
        public WixFeatureNode FeatureNode { get; }

        [NotNull]
        public ICollection<FileMapping> InstalledFiles { get; }

        [NotNull]
        public ICollection<Project> Projects { get; }

        [NotNull]
        public ICollection<ProjectOutputGroup> RequiredProjectOutputs { get; }

        [NotNull]
        public ICollection<ProjectOutputGroup> MissingProjectOutputs { get; }

        public FeatureMapping? Parent { get; set; }

        [NotNull, ItemNotNull]
        public ICollection<FeatureMapping> Children { get; } = new List<FeatureMapping>();
    }
}