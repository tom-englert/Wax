namespace tomenglertde.Wax.Model.Mapping
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.VisualStudio;
    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;

    public class FileMapping : ObservableObject
    {
        [NotNull]
        private readonly ProjectOutput _projectOutput;
        [NotNull]
        private readonly ObservableCollection<ProjectOutput> _allUnmappedProjectOutputs;
        [NotNull]
        private readonly ObservableFilteredCollection<ProjectOutput> _unmappedProjectOutputs;
        [NotNull]
        private readonly WixProject _wixProject;
        [NotNull]
        private readonly IList<UnmappedFile> _allUnmappedFiles;
        [NotNull]
        private readonly ObservableFilteredCollection<UnmappedFile> _unmappedFiles;
        [NotNull]
        private readonly string _id;

        private WixFileNode _mappedNode;
        private MappingState _mappingState;

        public FileMapping([NotNull] ProjectOutput projectOutput, ObservableCollection<ProjectOutput> allUnmappedProjectOutputs, [NotNull] WixProject wixProject, [NotNull] ObservableCollection<UnmappedFile> allUnmappedFiles)
        {
            Contract.Requires(projectOutput != null);
            Contract.Requires(wixProject != null);
            Contract.Requires(allUnmappedFiles != null);

            _projectOutput = projectOutput;
            _allUnmappedProjectOutputs = allUnmappedProjectOutputs;
            _wixProject = wixProject;
            _allUnmappedFiles = allUnmappedFiles;

            _id = wixProject.GetFileId(TargetName);

            MappedNode = wixProject.FileNodes.FirstOrDefault(node => node.Id == _id);

            _unmappedProjectOutputs = new ObservableFilteredCollection<ProjectOutput>(_allUnmappedProjectOutputs, item => string.Equals(item.FileName, DisplayName, StringComparison.OrdinalIgnoreCase));
            _unmappedProjectOutputs.CollectionChanged += UnmappedProjectOutputs_CollectionChanged;

            _unmappedFiles = new ObservableFilteredCollection<UnmappedFile>(allUnmappedFiles, item => string.Equals(item.Node.Name, DisplayName, StringComparison.OrdinalIgnoreCase));
            _unmappedFiles.CollectionChanged += UnmappedNodes_CollectionChanged;

            UpdateMappingState();
        }

        [NotNull]
        public string DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _projectOutput.FileName;
            }
        }

        [NotNull]
        public string Id
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _id;
            }
        }

        [NotNull]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _projectOutput.TargetName;
            }
        }

        [NotNull]
        public string Extension
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetExtension(_projectOutput.TargetName);
            }

        }

        [NotNull]
        public string TargetName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _projectOutput.TargetName;
            }
        }

        [NotNull]
        public string SourceName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _projectOutput.SourceName;
            }
        }


        [NotNull]
        public IList<UnmappedFile> UnmappedNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<UnmappedFile>>() != null);

                return _unmappedFiles;
            }
        }

        [NotNull]
        public ICommand AddFileCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<IEnumerable>(
                    _ => CanAddFile(),
                    selectedItems => selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.AddFile()));
            }
        }

        [NotNull]
        public ICommand ClearMappingCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<IEnumerable>(
                    _ => CanClearMapping(),
                    selectedItems => selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.ClearMapping()));
            }
        }

        [NotNull]
        public ICommand ResolveFileCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<IEnumerable>(
                    _ => CanResolveFile(),
                    selectedItems => selectedItems.Cast<FileMapping>().ToList().ForEach(fileMapping => fileMapping.ResolveFile()));
            }
        }

        [NotNull]
        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);

                return _projectOutput.Project;
            }
        }

        public WixFileNode MappedNodeSetter
        {
            get
            {
                Contract.Ensures(Contract.Result<WixFileNode>() == null);

                return null;
            }
            set
            {
                if (value != null)
                {
                    MappedNode = value;
                }
            }
        }

        public WixFileNode MappedNode
        {
            get
            {
                return _mappedNode;
            }
            set
            {
                SetProperty(ref _mappedNode, value, () => MappedNode, MappedNode_Changed);
            }
        }

        private void MappedNode_Changed(WixFileNode oldValue, WixFileNode newValue)
        {
            if (oldValue != null)
            {
                _allUnmappedFiles.Add(new UnmappedFile(oldValue, _allUnmappedFiles));
                _wixProject.UnmapFile(TargetName);
                _allUnmappedProjectOutputs.Add(_projectOutput);
            }

            if (newValue != null)
            {
                var unmappedFile = _allUnmappedFiles.FirstOrDefault(file => Equals(file.Node, newValue));
                _allUnmappedFiles.Remove(unmappedFile);
                _wixProject.MapFile(TargetName, newValue);
                _allUnmappedProjectOutputs.Remove(_projectOutput);
            }

            UpdateMappingState();
        }

        public MappingState MappingState
        {
            get
            {
                return _mappingState;
            }
            set
            {
                SetProperty(ref _mappingState, value, () => MappingState);
            }
        }

        private void UnmappedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingState();
        }

        void UnmappedProjectOutputs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingState();
        }

        private bool CanAddFile()
        {
            return (MappedNode == null) && !_unmappedFiles.Any();
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

        private void ClearMapping()
        {
            if (CanClearMapping())
            {
                MappedNode = null;
            }
        }

        private bool CanResolveFile()
        {
            Contract.Ensures((Contract.Result<bool>() == false) || (_unmappedFiles.Count == 1));

            return (MappedNode == null) && (_unmappedFiles.Count == 1);
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

        [NotNull]
        public static IEqualityComparer<FileMapping> Comparer
        {
            get
            {
                Contract.Ensures(Contract.Result<IEqualityComparer<FileMapping>>() != null);

                return new EqualityComparer();
            }
        }

        private class EqualityComparer : IEqualityComparer<FileMapping>
        {
            /// <summary>Determines whether the specified objects are equal.</summary>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            public bool Equals(FileMapping x, FileMapping y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                return x.UniqueName.Equals(y.UniqueName, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <returns>A hash code for the specified object.</returns>
            /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
            /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
            public int GetHashCode(FileMapping obj)
            {
                if (obj == null)
                    throw new ArgumentNullException("obj");

                return obj.UniqueName.GetHashCode();
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_id != null);
            Contract.Invariant(_projectOutput != null);
            Contract.Invariant(_wixProject != null);
            Contract.Invariant(_unmappedFiles != null);
            Contract.Invariant(_allUnmappedFiles != null);
            Contract.Invariant(_unmappedProjectOutputs != null);
            Contract.Invariant(_allUnmappedProjectOutputs != null);
        }
    }
}