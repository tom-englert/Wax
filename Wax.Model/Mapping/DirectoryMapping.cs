namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using PropertyChanged;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Wpf;

    [AddINotifyPropertyChangedInterface]
    public class DirectoryMapping
    {
        private readonly WixProject _wixProject;

        public DirectoryMapping(string directory, WixProject wixProject, IList<WixDirectoryNode> unmappedNodes)
        {
            Directory = directory;
            _wixProject = wixProject;
            Id = wixProject.GetDirectoryId(directory);
            UnmappedNodes = unmappedNodes;

            MappedNode = wixProject.DirectoryNodes.FirstOrDefault(node => node.Id == Id);
        }

        public string Directory { get; }

        public string Id { get; }

        public IList<WixDirectoryNode> UnmappedNodes { get; }

        public ICommand AddDirectoryCommand => new DelegateCommand(CanAddDirectory, AddDirectory);

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