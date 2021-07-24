namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using tomenglertde.Wax.Model.Mapping;
    using tomenglertde.Wax.Model.Tools;
    using tomenglertde.Wax.Model.VisualStudio;

    public class WixProject : Project
    {
        private static readonly string[] _wixFileExtensions = { ".wxs", ".wxi" };
        private static readonly string[] _wellKnownPublicMsiProperties = { "x86", "x64" };
        private const string WaxConfigurationFileExtension = ".wax";

        private readonly EnvDTE.ProjectItem _configurationFileProjectItem;
        private readonly ProjectConfiguration _configuration;
        private readonly IList<WixSourceFile> _sourceFiles;

        public WixProject(Solution solution, EnvDTE.Project project)
            : base(solution, project)
        {
            _configurationFileProjectItem = GetConfigurationFileProjectItem();

            var configurationText = _configurationFileProjectItem.GetContent();

            _configuration = configurationText.Deserialize<ProjectConfiguration>();

            Regex? excludedItemsFilter = null;

            try
            {
                if (!string.IsNullOrEmpty(_configuration.ExcludedProjectItems))
                {
                    excludedItemsFilter = new Regex(_configuration.ExcludedProjectItems);
                }
            }
            catch
            {
                // filter is corrupt, go with no filter.
            }

            _sourceFiles = GetAllProjectItems()
                .Where(item => _wixFileExtensions.Contains(Path.GetExtension(item.Name) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .Where(item => excludedItemsFilter == null || !excludedItemsFilter.IsMatch(item.Name))
                .OrderByDescending(item => Path.GetExtension(item.Name), StringComparer.OrdinalIgnoreCase)
                .Select(item => new WixSourceFile(this, item))
                .ToList().AsReadOnly();
        }

        public IEnumerable<WixSourceFile> SourceFiles => _sourceFiles;

        public IEnumerable<WixFileNode> FileNodes => _sourceFiles.SelectMany(sourceFile => sourceFile.FileNodes);

        public IEnumerable<WixDirectoryNode> DirectoryNodes => _sourceFiles.SelectMany(sourceFile => sourceFile.DirectoryNodes);

        public IEnumerable<WixFeatureNode> FeatureNodes => _sourceFiles.SelectMany(sourceFile => sourceFile.FeatureNodes);

        public IEnumerable<WixComponentNode> ComponentNodes => _sourceFiles.SelectMany(sourceFile => sourceFile.ComponentNodes);

        public IEnumerable<WixComponentGroupNode> ComponentGroupNodes => _sourceFiles.SelectMany(sourceFile => sourceFile.ComponentGroupNodes);

        public IEnumerable<Project> DeployedProjects
        {
            get
            {
                return Solution.Projects.Where(project => _configuration.DeployedProjectNames.Contains(project.UniqueName, StringComparer.OrdinalIgnoreCase));
            }
            set
            {
                var projects = value.ToList().AsReadOnly();
                var removedProjects = DeployedProjects.Except(projects).ToList().AsReadOnly();

                _configuration.DeployedProjectNames = projects.Select(project => project.UniqueName).ToArray();

                RemoveProjectReferences(removedProjects);
                AddProjectReferences(projects);

                SaveProjectConfiguration();
            }
        }

        public bool DeploySymbols
        {
            get => _configuration.DeploySymbols;
            set => _configuration.DeploySymbols = value;
        }

        public bool DeployLocalizations
        {
            get => _configuration.DeployLocalizations;
            set => _configuration.DeployLocalizations = value;
        }

        public bool DeployExternalLocalizations
        {
            get => _configuration.DeployExternalLocalizations;
            set => _configuration.DeployExternalLocalizations = value;
        }

        public bool HasChanges => HasConfigurationChanges | HasSourceFileChanges;

        public bool IsBootstrapper => WixExtensionReferences.Contains("WixBalExtension");

        public string GetDirectoryId(string directory)
        {
            return (_configuration.DirectoryMappings.TryGetValue(directory, out var value) && (value != null)) ? value : GetDefaultId(directory);
        }

        public void UnmapDirectory(string directory)
        {
            _configuration.DirectoryMappings.Remove(directory);

            SaveProjectConfiguration();
        }

        public void MapDirectory(string directory, WixDirectoryNode node)
        {
            MapElement(directory, node, _configuration.DirectoryMappings);
        }

        public WixDirectoryNode AddDirectoryNode(string directory)
        {
            var name = Path.GetFileName(directory);
            var id = GetDirectoryId(directory);
            var parentDirectoryName = Path.GetDirectoryName(directory);
            var parentId = string.IsNullOrEmpty(directory) ? string.Empty : GetDirectoryId(parentDirectoryName);

            var parent = DirectoryNodes.FirstOrDefault(node => node.Id.Equals(parentId));

            if (parent == null)
            {
                if (!string.IsNullOrEmpty(parentId))
                {
                    parent = AddDirectoryNode(parentDirectoryName);
                }
                else
                {
                    parentId = "TODO:" + Guid.NewGuid();
                    var sourceFile = _sourceFiles.First();
                    return sourceFile.AddDirectory(id, name, parentId);
                }
            }

            return parent.AddSubDirectory(id, name);
        }

        public bool HasDefaultDirectoryId(DirectoryMapping directoryMapping)
        {
            var directory = directoryMapping.Directory;

            var id = GetDirectoryId(directory);
            var defaultId = GetDefaultId(directory);

            return id == defaultId;
        }

        public string GetFileId(string filePath)
        {
            return (_configuration.FileMappings.TryGetValue(filePath, out var value) && value != null) ? value : GetDefaultId(filePath);
        }

        public void UnmapFile(string filePath)
        {
            _configuration.FileMappings.Remove(filePath);

            SaveProjectConfiguration();
        }

        public void MapFile(string filePath, WixFileNode node)
        {
            MapElement(filePath, node, _configuration.FileMappings);
        }

        public WixFileNode? AddFileNode(FileMapping fileMapping)
        {
            var targetName = fileMapping.TargetName;

            var name = Path.GetFileName(targetName);
            var id = GetFileId(targetName);
            var directoryName = Path.GetDirectoryName(targetName);
            var directoryId = GetDirectoryId(directoryName);
            var directory = DirectoryNodes.FirstOrDefault(node => node.Id.Equals(directoryId, StringComparison.OrdinalIgnoreCase));
            directoryId = directory?.Id ?? "TODO: unknown directory " + directoryName;

            var componentGroup = ForceComponentGroup(directoryId);

            if (componentGroup == null)
                return null;

            ForceFeatureRef(componentGroup.Id);

            return componentGroup.AddFileComponent(id, name, fileMapping);
        }

        private static string GetDefaultId(string path)
        {
            if (path.Length == 0)
                return "_";

            var s = new StringBuilder(path);

            for (var i = 0; i < s.Length; i++)
            {
                if (!IsValidForId(s[i]))
                {
                    s[i] = '_';
                }
            }

            if (char.IsDigit(s[0]))
            {
                s.Insert(0, '_');
            }

            if (_wellKnownPublicMsiProperties.Contains(s.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                s.Insert(0, '_');
            }

            return s.ToString();
        }

        private WixComponentGroupNode? ForceComponentGroup(string directoryId)
        {
            return ComponentGroupNodes.FirstOrDefault(group => group.Directory == directoryId) ?? _sourceFiles.FirstOrDefault()?.AddComponentGroup(directoryId);
        }

        private void ForceFeatureRef(string componentGroupId)
        {
            if (FeatureNodes.Any(feature => feature.ComponentGroupRefs.Contains(componentGroupId)))
                return;

            var firstFeature = FeatureNodes.FirstOrDefault();
            if (firstFeature == null)
                return;

            firstFeature.AddComponentGroupRef(componentGroupId);
            firstFeature.SourceFile.Save();
        }

        public bool HasDefaultFileId(FileMapping fileMapping)
        {
            var filePath = fileMapping.TargetName;
            var id = GetFileId(filePath);
            var defaultId = GetDefaultId(filePath);

            return id == defaultId;
        }

        private void MapElement(string path, WixNode node, IDictionary<string, string> mappings)
        {
            if (node.Id.Equals(GetDefaultId(path)))
                mappings.Remove(path);
            else
                mappings[path] = node.Id;

            SaveProjectConfiguration();
        }

        private static bool IsValidForId(char value)
        {
            return (value <= 'z') && (char.IsLetterOrDigit(value) || (value == '_') || (value == '.'));
        }

        private bool HasConfigurationChanges => (_configuration.Serialize() != _configurationFileProjectItem.GetContent());

        private bool HasSourceFileChanges => _sourceFiles.Any(sourceFile => sourceFile.HasChanges);

        private void SaveProjectConfiguration()
        {
            var configurationText = _configuration.Serialize();

            if (configurationText != _configurationFileProjectItem.GetContent())
                _configurationFileProjectItem.SetContent(configurationText);
        }

        private EnvDTE.ProjectItem GetConfigurationFileProjectItem()
        {
            var configurationFileProjectItem = GetAllProjectItems().FirstOrDefault(item => WaxConfigurationFileExtension.Equals(Path.GetExtension(item.Name), StringComparison.OrdinalIgnoreCase));

            if (configurationFileProjectItem != null)
                return configurationFileProjectItem;

            var configurationFileName = Path.ChangeExtension(FullName, WaxConfigurationFileExtension);

            if (!File.Exists(configurationFileName))
                File.WriteAllText(configurationFileName, new ProjectConfiguration().Serialize());

            return AddItemFromFile(configurationFileName);
        }
    }
}