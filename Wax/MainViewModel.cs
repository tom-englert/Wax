namespace tomenglertde.Wax
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
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
            DeployExternalLocalizations = newValue.DeployExternalLocalizations;

            var deployedProjects = newValue.DeployedProjects.ToArray();

            CanHideReferencedProjects = deployedProjects.All(p => Solution.TopLevelProjects.Contains(p));

            _selectedVSProjects.Clear();
            _selectedVSProjects.AddRange(deployedProjects);

            GenerateMappings(deployedProjects, newValue);

            _wixProjectChanging = false;
        }

        [NotNull]
        public IList SelectedVSProjects => _selectedVSProjects;

        [CanBeNull]
        public DirectoryMapping InstallDirectoryMapping { get; set; }

        [CanBeNull, ItemNotNull]
        public IList<DirectoryMapping> DirectoryMappings { get; set; }

        [CanBeNull, ItemNotNull]
        public IList<FileMapping> FileMappings { get; set; }

        public bool CanHideReferencedProjects { get; set; }

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

        public bool AreAllDirectoriesMapped => InstallDirectoryMapping?.MappedNode != null && DirectoryMappings != null && DirectoryMappings.All(item => item?.MappedNode != null);

        public bool AreAllFilesMapped => FileMappings != null && FileMappings.All(item => item?.MappedNode != null);

        [NotNull]
        public static IEnumerable<BuildFileGroups> ProjectOutputs => Enum.GetValues(typeof(BuildFileGroups)).Cast<BuildFileGroups>().Where(item => item != 0);

        private void SelectedVSProjects_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            CanHideReferencedProjects = _selectedVSProjects.All(p => Solution.TopLevelProjects.Contains(p));

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

            GenerateMappings(vsProjects.Cast<Project>().ToArray(), wixProject);
        }

        private void GenerateMappings([CanBeNull, ItemNotNull] IList<Project> vsProjects, [CanBeNull] WixProject wixProject)
        {
            if ((vsProjects == null) || (wixProject == null))
                return;

            try
            {
                var projectOutputs = vsProjects
                    .SelectMany(project => project.GetProjectOutput(DeploySymbols, DeployLocalizations, DeployExternalLocalizations))
                    .ToArray();

                GenerateDirectoryMappings(projectOutputs, wixProject);
                GenerateFileMappings(projectOutputs, wixProject);
            }
            catch
            {
                // solution is still loading....
            }
        }

        private void GenerateFileMappings([NotNull, ItemNotNull] IEnumerable<ProjectOutput> projectOutputs, [NotNull] WixProject wixProject)
        {
            var unmappedFileNodes = new ObservableCollection<UnmappedFile>();
            unmappedFileNodes.AddRange(wixProject.FileNodes.Select(node => new UnmappedFile(node, unmappedFileNodes)));

            projectOutputs = projectOutputs
                .OrderBy(item => item.IsReference ? 1 : 0)
                .Distinct()
                .ToArray();

            var unmappedProjectOutputs = new ObservableCollection<ProjectOutput>(projectOutputs);

            // ReSharper disable once AssignNullToNotNullAttribute
            FileMappings = projectOutputs.Select(projectOutput => new FileMapping(projectOutput, unmappedProjectOutputs, wixProject, unmappedFileNodes))
                .ToArray();

            UnmappedFileNodes = unmappedFileNodes;
        }

        private void GenerateDirectoryMappings([NotNull, ItemNotNull] IEnumerable<ProjectOutput> projectOutputs, [NotNull] WixProject wixProject)
        {
            var directories = projectOutputs
                .Select(projectOutput => Path.GetDirectoryName(projectOutput.TargetName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .DefaultIfEmpty(string.Empty)
                .ToArray();

            var unmappedDirectoryNodes = new ObservableCollection<WixDirectoryNode>();

            // ReSharper disable once AssignNullToNotNullAttribute
            var directoryMappings = directories
                .Select(dir => new DirectoryMapping(dir, wixProject, unmappedDirectoryNodes))
                .ToArray();

            InstallDirectoryMapping = directoryMappings.FirstOrDefault();
            DirectoryMappings = directoryMappings.Skip(1).ToArray();

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
