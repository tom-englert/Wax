namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;

    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    public class FeatureMapping
    {
        public FeatureMapping(WixFeatureNode featureNode, ICollection<FileMapping> installedFiles,
            ICollection<Project> projects, ICollection<ProjectOutputGroup> requiredProjectOutputs, ICollection<ProjectOutputGroup> missingProjectOutputs)
        {
            FeatureNode = featureNode;
            InstalledFiles = installedFiles;
            Projects = projects;
            RequiredProjectOutputs = requiredProjectOutputs;
            MissingProjectOutputs = missingProjectOutputs;
        }

        public WixFeatureNode FeatureNode { get; }

        public ICollection<FileMapping> InstalledFiles { get; }

        public ICollection<Project> Projects { get; }

        public ICollection<ProjectOutputGroup> RequiredProjectOutputs { get; }

        public ICollection<ProjectOutputGroup> MissingProjectOutputs { get; }

        public FeatureMapping? Parent { get; set; }

        public ICollection<FeatureMapping> Children { get; } = new List<FeatureMapping>();
    }
}