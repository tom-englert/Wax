namespace tomenglertde.Wax
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Mapping;
    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class MainViewModel : DependencyObject, INotifyPropertyChanged
    {
        private bool _wixProjectChanging;
        [NotNull]
        private readonly Solution _solution;
        private readonly ObservableCollection<Project> _selectedVSProjects = new ObservableCollection<Project>();

        public MainViewModel([NotNull] EnvDTE.Solution solution)
        {
            Contract.Requires(solution != null);

            _selectedVSProjects.CollectionChanged += SelectedVSProjects_CollectionChanged;
            _solution = new Solution(solution);

            CommandManager.RequerySuggested += CommandManager_RequerySuggested;
        }

        [NotNull]
        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        public WixProject SelectedWixProject
        {
            get { return (WixProject)GetValue(SelectedWixProjectProperty); }
            set { SetValue(SelectedWixProjectProperty, value); }
        }
        /// <summary>
        /// Identifies the SelectedWixProject dependency property
        /// </summary>
        public static readonly DependencyProperty SelectedWixProjectProperty =
            DependencyProperty.Register("SelectedWixProject", typeof(WixProject), typeof(MainViewModel), new FrameworkPropertyMetadata(null, (sender, e) => ((MainViewModel)sender).SelectedWixProject_Changed((WixProject)e.NewValue)));


        [NotNull]
        public IList SelectedVSProjects
        {
            get
            {
                Contract.Ensures(Contract.Result<IList>() != null);

                return _selectedVSProjects;
            }
        }


        public DirectoryMapping InstallDirectoryMapping
        {
            get { return (DirectoryMapping)GetValue(InstallDirectoryMappingProperty); }
            set { SetValue(InstallDirectoryMappingProperty, value); }
        }

        /// <summary>
        /// Identifies the InstallDirectoryMapping dependency property
        /// </summary>
        public static readonly DependencyProperty InstallDirectoryMappingProperty =
            DependencyProperty.Register("InstallDirectoryMapping", typeof(DirectoryMapping), typeof(MainViewModel));


        public IList<DirectoryMapping> DirectoryMappings
        {
            get { return (IList<DirectoryMapping>)GetValue(DirectoryMappingsProperty); }
            set { SetValue(DirectoryMappingsProperty, value); }
        }
        /// <summary>
        /// Identifies the DirectoryMappings dependency property
        /// </summary>
        public static readonly DependencyProperty DirectoryMappingsProperty =
            DependencyProperty.Register("DirectoryMappings", typeof(IList<DirectoryMapping>), typeof(MainViewModel));


        public IList<FileMapping> FileMappings
        {
            get { return (IList<FileMapping>)GetValue(FileMappingsProperty); }
            set { SetValue(FileMappingsProperty, value); }
        }
        /// <summary>
        /// Identifies the FileMappings dependency property
        /// </summary>
        public static readonly DependencyProperty FileMappingsProperty =
            DependencyProperty.Register("FileMappings", typeof(IList<FileMapping>), typeof(MainViewModel));


        public bool CanHideReferencedProjects
        {
            get { return this.GetValue<bool>(CanHideReferencedProjectsProperty); }
            set { SetValue(CanHideReferencedProjectsProperty, value); }
        }
        /// <summary>
        /// Identifies the CanHideReferencedProjects dependency property
        /// </summary>
        public static readonly DependencyProperty CanHideReferencedProjectsProperty =
            DependencyProperty.Register("CanHideReferencedProjects", typeof(bool), typeof(MainViewModel));


        public bool DeploySymbols
        {
            get { return this.GetValue<bool>(DeploySymbolsProperty); }
            set { SetValue(DeploySymbolsProperty, value); }
        }
        /// <summary>
        /// Identifies the DeploySymbols dependency property
        /// </summary>
        public static readonly DependencyProperty DeploySymbolsProperty =
            DependencyProperty.Register("DeploySymbols", typeof(bool), typeof(MainViewModel), new FrameworkPropertyMetadata(false, (sender, e) => ((MainViewModel)sender).DeploySymbols_Changed((bool)e.NewValue)));


        public bool DeployExternalLocalizations
        {
            get { return (bool)GetValue(DeployExternalLocalizationsProperty); }
            set { SetValue(DeployExternalLocalizationsProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="DeployExternalLocalizations"/> dependency property
        /// </summary>
        public static readonly DependencyProperty DeployExternalLocalizationsProperty =
            DependencyProperty.Register("DeployExternalLocalizations", typeof(bool), typeof(MainViewModel), new FrameworkPropertyMetadata(false, (sender, e) => ((MainViewModel)sender).DeployExternalLocalizations_Changed((bool)e.NewValue)));


        public IList<UnmappedFile> UnmappedFileNodes
        {
            get { return (IList<UnmappedFile>)GetValue(UnmappedFileNodesProperty); }
            set { SetValue(UnmappedFileNodesProperty, value); }
        }
        /// <summary>
        /// Identifies the UnmappedFileNodes dependency property
        /// </summary>
        public static readonly DependencyProperty UnmappedFileNodesProperty =
            DependencyProperty.Register("UnmappedFileNodes", typeof(IList<UnmappedFile>), typeof(MainViewModel));

        public bool HasExternalChanges => ((SelectedWixProject != null) && (SelectedWixProject.HasChanges));

        public bool AreAllDirectoriesMapped
        {
            get
            {
                return InstallDirectoryMapping != null && InstallDirectoryMapping.MappedNode != null && DirectoryMappings != null && DirectoryMappings.All(item => item.MappedNode != null);
            }
        }

        public bool AreAllFilesMapped
        {
            get
            {
                return FileMappings != null && FileMappings.All(item => item.MappedNode != null);
            }
        }

        [NotNull]
        public static IEnumerable<BuildFileGroups> ProjectOutputs
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BuildFileGroups>>() != null);

                return Enum.GetValues(typeof(BuildFileGroups)).Cast<BuildFileGroups>().Where(item => item != 0);
            }
        }

        private void SelectedWixProject_Changed(WixProject newValue)
        {
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

        private void SelectedVSProjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        private void GenerateMappings(IEnumerable vsProjects, WixProject wixProject)
        {
            if (vsProjects == null)
                return;

            GenerateMappings(vsProjects.Cast<Project>().ToArray(), wixProject);
        }

        private void GenerateMappings([ItemNotNull] IList<Project> vsProjects, WixProject wixProject)
        {
            if ((vsProjects == null) || (wixProject == null))
                return;

            try
            {
                var projectOutputs = vsProjects
                    .SelectMany(project => project.GetProjectOutput(DeploySymbols, DeployExternalLocalizations))
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
            Contract.Requires(projectOutputs != null);
            Contract.Requires(wixProject != null);

            var unmappedFileNodes = new ObservableCollection<UnmappedFile>();
            unmappedFileNodes.AddRange(wixProject.FileNodes.Select(node => new UnmappedFile(node, unmappedFileNodes)));

            projectOutputs = projectOutputs
                .OrderBy(item => item.IsReference ? 1 : 0)
                .Distinct()
                .ToArray();

            var unmappedProjectOutputs = new ObservableCollection<ProjectOutput>(projectOutputs);

            FileMappings = projectOutputs.Select(projectOutput => new FileMapping(projectOutput, unmappedProjectOutputs, wixProject, unmappedFileNodes))
                .ToArray();

            UnmappedFileNodes = unmappedFileNodes;
        }

        private void GenerateDirectoryMappings([NotNull, ItemNotNull] IEnumerable<ProjectOutput> projectOutputs, [NotNull] WixProject wixProject)
        {
            Contract.Requires(projectOutputs != null);
            Contract.Requires(wixProject != null);

            var directories = projectOutputs
                .Select(projectOutput => Path.GetDirectoryName(projectOutput.TargetName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .DefaultIfEmpty(string.Empty)
                .ToArray();

            var unmappedDirectoryNodes = new ObservableCollection<WixDirectoryNode>();

            var directoryMappings = directories.Select(dir => new DirectoryMapping(dir, wixProject, unmappedDirectoryNodes)).ToArray();

            InstallDirectoryMapping = directoryMappings.FirstOrDefault();
            DirectoryMappings = directoryMappings.Skip(1).ToArray();

            var unmappedNodes = wixProject.DirectoryNodes.Where(node => directoryMappings.All(mapping => mapping.Id != node.Id));

            foreach (var node in unmappedNodes)
            {
                unmappedDirectoryNodes.Add(node);
            }
        }

        private void DeploySymbols_Changed(bool newValue)
        {
            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeploySymbols = newValue;

            GenerateMappings(SelectedVSProjects, wixProject);
        }

        private void DeployExternalLocalizations_Changed(bool newValue)
        {
            if (_wixProjectChanging)
                return;

            var wixProject = SelectedWixProject;

            if (wixProject == null)
                return;

            wixProject.DeployExternalLocalizations = newValue;

            GenerateMappings(SelectedVSProjects, wixProject);
        }


        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            OnPropertyChanged("AreAllDirectoriesMapped");
            OnPropertyChanged("AreAllFilesMapped");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([NotNull] string propertyName)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));

            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
