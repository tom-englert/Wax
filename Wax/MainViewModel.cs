namespace tomenglertde.Wax
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Mapping;
    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Core;

    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private bool _wixProjectChanging;

        [NotNull]
        private readonly ObservableCollection<Project> _selectedVSProjects = new ObservableCollection<Project>();

        public MainViewModel([NotNull] EnvDTE.Solution solution)
        {
            _selectedVSProjects.CollectionChanged += SelectedVSProjects_CollectionChanged;
            Solution = new Solution(solution);

            CommandManager.RequerySuggested += CommandManager_RequerySuggested;
        }

        [NotNull]
        public Solution Solution { get; }

        [CanBeNull]
        public WixProject SelectedWixProject { get; set; }

        [UsedImplicitly]
        private void OnSelectedWixProjectChanged()
        {
            var newValue = SelectedWixProject;
            if (newValue == null)
                return;

            _wixProjectChanging = true;

            DeploySymbols = newValue.DeploySymbols;
            DeployLocalizations = newValue.DeployLocalizations;
            DeployExternalLocalizations = newValue.DeployExternalLocalizations;

            var deployedProjects = newValue.DeployedProjects.ToList().AsReadOnly();

            var topLevelProjects = new HashSet<Project>(Solution.EnumerateTopLevelProjects);
            CanHideReferencedProjects = deployedProjects.All(p => topLevelProjects.Contains(p));

            _selectedVSProjects.Clear();
            _selectedVSProjects.AddRange(deployedProjects);

            GenerateMappings(deployedProjects, newValue);

            _wixProjectChanging = false;
        }

        [NotNull]
        public IList SelectedVSProjects => _selectedVSProjects;

        [CanBeNull]
        public DirectoryMapping InstallDirectoryMapping { get; private set; }

        [CanBeNull, ItemNotNull]
        public IList<DirectoryMapping> DirectoryMappings { get; private set; }

        [CanBeNull, ItemNotNull]
        public IList<FileMapping> FileMappings { get; private set; }

        [CanBeNull, ItemNotNull]
        public ICollection<FeatureMapping> FeatureMappings { get; private set; }

        public bool CanHideReferencedProjects { get; private set; }

        public bool DeploySymbols { get; set; }

        [UsedImplicitly]
        private void OnDeploySymbolsChanged()
        {
            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeploySymbols = DeploySymbols;

            GenerateMappings(SelectedVSProjects, wixProject);
        }

        public bool DeployLocalizations { get; set; }

        [UsedImplicitly]
        private void OnDeployLocalizationsChanged()
        {
            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeployLocalizations = DeployLocalizations;

            GenerateMappings(SelectedVSProjects, wixProject);
        }

        public bool DeployExternalLocalizations { get; set; }

        [UsedImplicitly]
        private void OnDeployExternalLocalizationsChanged()
        {
            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeployExternalLocalizations = DeployExternalLocalizations;

            GenerateMappings(SelectedVSProjects, wixProject);
        }

        public IList<UnmappedFile> UnmappedFileNodes { get; set; }

        public bool HasExternalChanges => ((SelectedWixProject != null) && (SelectedWixProject.HasChanges));

        public bool AreAllDirectoriesMapped => InstallDirectoryMapping?.MappedNode != null && DirectoryMappings != null && DirectoryMappings.All(item => item.MappedNode != null);

        public bool AreAllFilesMapped => FileMappings != null && FileMappings.All(item => item.MappedNode != null);

        public bool IsUpdating { get; set; }

        [NotNull]
        public static IEnumerable<BuildFileGroups> ProjectOutputs => Enum.GetValues(typeof(BuildFileGroups)).Cast<BuildFileGroups>().Where(item => item != 0);

        private void SelectedVSProjects_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            var topLevelProjects = new HashSet<Project>(Solution.EnumerateTopLevelProjects);

            CanHideReferencedProjects = _selectedVSProjects.All(p => topLevelProjects.Contains(p));

            var vsProjects = Solution.Projects.Where(p => p.IsVsProject);

            foreach (var project in vsProjects)
            {
                project.UpdateIsImplicitSelected(new HashSet<Project>(_selectedVSProjects));
            }

            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeployedProjects = _selectedVSProjects;

            GenerateMappings(_selectedVSProjects, wixProject);
        }

        private void GenerateMappings([CanBeNull, ItemNotNull] IEnumerable vsProjects, [CanBeNull] WixProject wixProject)
        {
            if (vsProjects == null)
                return;

            GenerateMappings(vsProjects.Cast<Project>().ToList().AsReadOnly(), wixProject);
        }

        private void GenerateMappings([CanBeNull, ItemNotNull] IList<Project> vsProjects, [CanBeNull] WixProject wixProject)
        {
            if ((vsProjects == null) || (wixProject == null))
                return;

            try
            {
                IsUpdating = true;

                var projectOutputs = vsProjects
                    .SelectMany(project => project.GetProjectOutput(DeploySymbols, DeployLocalizations, DeployExternalLocalizations))
                    .ToList().AsReadOnly();

                // ReSharper disable PossibleNullReferenceException
                // ReSharper disable AssignNullToNotNullAttribute
                var projectOutputGroups = projectOutputs
                    .GroupBy(item => item.TargetName)
                    .Select(group => new ProjectOutputGroup(group.Key, group.OrderBy(item => item.IsReference ? 1 : 0).ToList().AsReadOnly()))
                    .ToList().AsReadOnly();
                // ReSharper restore AssignNullToNotNullAttribute
                // ReSharper restore PossibleNullReferenceException

                GenerateDirectoryMappings(projectOutputs, wixProject);
                GenerateFileMappings(projectOutputGroups, wixProject);
                GenerateFeatureMappings(projectOutputGroups, vsProjects, wixProject);
            }
            catch
            {
                // solution is still loading....
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private void GenerateFeatureMappings([NotNull, ItemNotNull] IList<ProjectOutputGroup> projectOutputGroups, [NotNull, ItemNotNull] IList<Project> vsProjects, [NotNull] WixProject wixProject)
        {
            Debug.Assert(FileMappings != null);

            var componentNodes = wixProject.ComponentNodes.ToDictionary(node => node.Id);
            var componentGroupNodes = wixProject.ComponentGroupNodes.ToDictionary(node => node.Id);
            var fileNodes = wixProject.FileNodes.ToDictionary(node => node.Id);
            var fileMappingsLookup = FileMappings.ToDictionary(fm => fm.Id);
            var featureMappings = new List<FeatureMapping>();

            foreach (var featureNode in wixProject.FeatureNodes)
            {
                var installedFileNodes = featureNode.EnumerateInstalledFiles(componentGroupNodes, componentNodes, fileNodes)
                    .ToList().AsReadOnly();

                var fileMappings = installedFileNodes
                    // ReSharper disable once PossibleNullReferenceException
                    .Select(file => fileMappingsLookup.GetValueOrDefault(file.Id))
                    .Where(item => item != null)
                    .ToList().AsReadOnly();

                // ReSharper disable once PossibleNullReferenceException
                var installedTargetNames = new HashSet<string>(fileMappings.Select(fm => fm.TargetName));

                var projects = vsProjects
                    .Where(p => installedTargetNames.Contains(p.PrimaryOutputFileName))
                    .ToList().AsReadOnly();

                var requiredOutputs = projectOutputGroups
                    .Where(group => projects.Any(proj => group.Projects.Contains(proj)))
                    .ToList().AsReadOnly();

                var missingOutputs = requiredOutputs
                    // ReSharper disable once PossibleNullReferenceException
                    .Where(o => !installedTargetNames.Contains(o.TargetName))
                    .ToList().AsReadOnly();

                featureMappings.Add(new FeatureMapping(featureNode, fileMappings, projects, requiredOutputs, missingOutputs));
            }

            // ReSharper disable once PossibleNullReferenceException
            var featureMappingsLookup = featureMappings.ToDictionary(item => item.FeatureNode);

            foreach (var featureMapping in featureMappings)
            {
                var parentNode = featureMapping.FeatureNode.Parent;
                if (parentNode == null)
                    continue;

                featureMapping.Parent = featureMappingsLookup.GetValueOrDefault(parentNode);
            }

            foreach (var featureMapping in featureMappings)
            {
                featureMapping.Parent?.Children.Add(featureMapping);
            }

            FeatureMappings = featureMappings
                .Where(feature => feature?.Parent == null)
                .ToList().AsReadOnly();
        }

        private void GenerateFileMappings([NotNull, ItemNotNull] IList<ProjectOutputGroup> projectOutputGroups, [NotNull] WixProject wixProject)
        {
            var unmappedFileNodes = new ObservableCollection<UnmappedFile>();
            unmappedFileNodes.AddRange(wixProject.FileNodes.Select(node => new UnmappedFile(node, unmappedFileNodes)));

            var unmappedProjectOutputs = new ObservableCollection<ProjectOutputGroup>(projectOutputGroups);

            // ReSharper disable once AssignNullToNotNullAttribute
            FileMappings = projectOutputGroups.Select(projectOutput => new FileMapping(projectOutput, unmappedProjectOutputs, wixProject, unmappedFileNodes))
                .ToList().AsReadOnly();

            UnmappedFileNodes = unmappedFileNodes;
        }

        private void GenerateDirectoryMappings([NotNull, ItemNotNull] IEnumerable<ProjectOutput> projectOutputs, [NotNull] WixProject wixProject)
        {
            var directories = projectOutputs
                .Select(projectOutput => Path.GetDirectoryName(projectOutput.TargetName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .DefaultIfEmpty(string.Empty)
                .ToList().AsReadOnly();

            var unmappedDirectoryNodes = new ObservableCollection<WixDirectoryNode>();

            // ReSharper disable once AssignNullToNotNullAttribute
            var directoryMappings = directories
                .Select(dir => new DirectoryMapping(dir, wixProject, unmappedDirectoryNodes))
                .ToList().AsReadOnly();

            InstallDirectoryMapping = directoryMappings.FirstOrDefault();
            DirectoryMappings = directoryMappings.Skip(1).ToList().AsReadOnly();

            // ReSharper disable once PossibleNullReferenceException
            var unmappedNodes = wixProject.DirectoryNodes.Where(node => directoryMappings.All(mapping => mapping.Id != node.Id));

            foreach (var node in unmappedNodes)
            {
                unmappedDirectoryNodes.Add(node);
            }
        }

        private void CommandManager_RequerySuggested([NotNull] object sender, [NotNull] EventArgs e)
        {
            OnPropertyChanged(nameof(AreAllDirectoriesMapped));
            OnPropertyChanged(nameof(AreAllFilesMapped));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
