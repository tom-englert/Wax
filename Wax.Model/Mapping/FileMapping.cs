namespace tomenglertde.Wax.Model.Mapping
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;

    using PropertyChanged;

    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;

    [AddINotifyPropertyChangedInterface]
    public class FileMapping
    {
        private readonly ProjectOutputGroup _projectOutputGroup;
        private readonly ObservableCollection<ProjectOutputGroup> _allUnmappedProjectOutputs;
        private readonly ObservableFilteredCollection<ProjectOutputGroup> _unmappedProjectOutputs;
        private readonly WixProject _wixProject;
        private readonly IList<UnmappedFile> _allUnmappedFiles;
        private readonly ObservableFilteredCollection<UnmappedFile> _unmappedFiles;

        public FileMapping(ProjectOutputGroup projectOutputGroup, ObservableCollection<ProjectOutputGroup> allUnmappedProjectOutputs, WixProject wixProject, IList<UnmappedFile> allUnmappedFiles)
        {
            _projectOutputGroup = projectOutputGroup;
            _allUnmappedProjectOutputs = allUnmappedProjectOutputs;
            _wixProject = wixProject;
            _allUnmappedFiles = allUnmappedFiles;

            Id = wixProject.GetFileId(TargetName);

            MappedNode = wixProject.FileNodes.FirstOrDefault(node => node.Id == Id);

            _unmappedProjectOutputs = new ObservableFilteredCollection<ProjectOutputGroup>(_allUnmappedProjectOutputs, item => string.Equals(item?.FileName, DisplayName, StringComparison.OrdinalIgnoreCase));
            _unmappedProjectOutputs.CollectionChanged += UnmappedProjectOutputs_CollectionChanged;

            _unmappedFiles = new ObservableFilteredCollection<UnmappedFile>(allUnmappedFiles, item => string.Equals(item?.Node.Name, DisplayName, StringComparison.OrdinalIgnoreCase));
            _unmappedFiles.CollectionChanged += UnmappedNodes_CollectionChanged;

            UpdateMappingState();
        }

        public string DisplayName => _projectOutputGroup.FileName;

        public string Id { get; }

        public string UniqueName => _projectOutputGroup.TargetName;

        public string Extension => Path.GetExtension(_projectOutputGroup.TargetName);

        public string TargetName => _projectOutputGroup.TargetName;

        public string SourceName => _projectOutputGroup.SourceName;

        public IList<UnmappedFile> UnmappedNodes => _unmappedFiles;

        [DoNotNotify]
        public ICommand AddFileCommand => new DelegateCommand<IEnumerable>(_ => CanAddFile(), AddFile);

        [DoNotNotify]
        public ICommand ClearMappingCommand => new DelegateCommand<IEnumerable>(_ => CanClearMapping(), ClearMapping);

        [DoNotNotify]
        public ICommand ResolveFileCommand => new DelegateCommand<IEnumerable>(_ => CanResolveFile(), ResolveFile);

        public Project Project =>
            _projectOutputGroup.ProjectOutputs
                .Select(output => output.Project)
                .SortByRelevance()
                .First();

        public Project TopLevelProject
        {
            get
            {
                var project = Project;
                while (true)
                {
                    var referencedBy = project.ImplicitSelectedByProjects.SortByRelevance().FirstOrDefault();
                    if (referencedBy == null)
                        return project;
                    project = referencedBy;
                }
            }
        }

        public WixFileNode? MappedNodeSetter
        {
            get => null;
            set
            {
                if (value != null)
                {
                    MappedNode = value;
                }
            }
        }

        [OnChangedMethod(nameof(OnMappedNodeChanged))]
        public WixFileNode? MappedNode { get; set; }

        private void OnMappedNodeChanged(WixFileNode? oldValue, WixFileNode? newValue)
        {
            if (oldValue != null)
            {
                _allUnmappedFiles.Add(new UnmappedFile(oldValue, _allUnmappedFiles));
                _wixProject.UnmapFile(TargetName);
                _allUnmappedProjectOutputs.Add(_projectOutputGroup);
            }

            if (newValue != null)
            {
                var unmappedFile = _allUnmappedFiles.FirstOrDefault(file => Equals(file.Node, newValue));
                _allUnmappedFiles.Remove(unmappedFile);
                _wixProject.MapFile(TargetName, newValue);
                _allUnmappedProjectOutputs.Remove(_projectOutputGroup);
            }

            UpdateMappingState();
        }

        public MappingState MappingState { get; set; }

        private void UnmappedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingState();
        }

        private void UnmappedProjectOutputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingState();
        }

        private bool CanAddFile()
        {
            return (MappedNode == null) && !_unmappedFiles.Any();
        }

        private static void AddFile(IEnumerable selectedItems)
        {
            selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.AddFile());
        }

        private void AddFile()
        {
            if (CanAddFile())
            {
                MappedNode = _wixProject.AddFileNode(this);
            }
        }

        private bool CanClearMapping()
        {
            return (MappedNode != null) && (!_wixProject.HasDefaultFileId(this));
        }

        private static void ClearMapping(IEnumerable selectedItems)
        {
            selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.ClearMapping());
        }

        private void ClearMapping()
        {
            if (CanClearMapping())
            {
                MappedNode = null;
            }
        }

        private bool CanResolveFile()
        {
            return (MappedNode == null) && (_unmappedFiles.Count == 1);
        }

        private static void ResolveFile(IEnumerable selectedItems)
        {
            selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.ResolveFile());
        }

        private void ResolveFile()
        {
            if (CanResolveFile())
            {
                MappedNode = _unmappedFiles[0];
            }
        }

        private void UpdateMappingState()
        {
            if (MappedNode != null)
            {
                MappingState = MappingState.Resolved;
                return;
            }

            switch (_unmappedFiles.Count)
            {
                case 0:
                    MappingState = MappingState.Unmapped;
                    return;

                case 1:
                    if (_unmappedProjectOutputs.Count == 1)
                    {
                        MappingState = MappingState.Unique;
                        return;
                    }
                    break;
            }

            MappingState = MappingState.Ambiguous;
        }
    }
}