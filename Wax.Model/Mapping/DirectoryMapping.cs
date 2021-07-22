﻿namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using PropertyChanged;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Wpf;

    [AddINotifyPropertyChangedInterface]
    public class DirectoryMapping
    {
        [NotNull]
        private readonly WixProject _wixProject;

        public DirectoryMapping([NotNull] string directory, [NotNull] WixProject wixProject, [NotNull] IList<WixDirectoryNode> unmappedNodes)
        {
            Directory = directory;
            _wixProject = wixProject;
            Id = wixProject.GetDirectoryId(directory);
            UnmappedNodes = unmappedNodes;

            MappedNode = wixProject.DirectoryNodes.FirstOrDefault(node => node.Id == Id);
        }

        [NotNull]
        public string Directory { get; }

        [NotNull]
        public string Id { get; }

        [NotNull]
        public IList<WixDirectoryNode> UnmappedNodes { get; }

        [NotNull]
        public ICommand AddDirectoryCommand => new DelegateCommand(CanAddDirectory, AddDirectory);

        [NotNull]
        public ICommand ClearMappingCommand => new DelegateCommand(CanClearMapping, ClearMapping);

        public WixDirectoryNode? MappedNodeSetter
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
        public WixDirectoryNode? MappedNode { get; set; }

        private void OnMappedNodeChanged(WixDirectoryNode? oldValue, WixDirectoryNode? newValue)
        {
            if (oldValue != null)
            {
                UnmappedNodes.Add(oldValue);
                _wixProject.UnmapDirectory(Directory);
            }

            if (newValue != null)
            {
                UnmappedNodes.Remove(newValue);
                _wixProject.MapDirectory(Directory, newValue);
            }
        }

        private void AddDirectory()
        {
            MappedNode = _wixProject.AddDirectoryNode(Directory);
        }

        private bool CanAddDirectory()
        {
            return (MappedNode == null);
        }

        private void ClearMapping()
        {
            MappedNode = null;
        }

        private bool CanClearMapping()
        {
            return (MappedNode != null) && (!_wixProject.HasDefaultDirectoryId(this));
        }
    }
}