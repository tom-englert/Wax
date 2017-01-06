namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    public class DirectoryMapping : ObservableObject
    {
        [NotNull]
        private readonly string _directory;
        private readonly string _id;
        [NotNull]
        private readonly IList<WixDirectoryNode> _unmappedNodes;
        [NotNull]
        private readonly WixProject _wixProject;

        private WixDirectoryNode _mappedNode;

        public DirectoryMapping([NotNull] string directory, [NotNull] WixProject wixProject, [NotNull] IList<WixDirectoryNode> unmappedNodes)
        {
            Contract.Requires(directory != null);
            Contract.Requires(wixProject != null);
            Contract.Requires(unmappedNodes != null);

            _directory = directory;
            _wixProject = wixProject;
            _id = wixProject.GetDirectoryId(directory);
            _unmappedNodes = unmappedNodes;

            MappedNode = wixProject.DirectoryNodes.FirstOrDefault(node => node.Id == _id);
        }

        [NotNull]
        public string Directory
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _directory;
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
        public IList<WixDirectoryNode> UnmappedNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<WixDirectoryNode>>() != null);

                return _unmappedNodes;
            }
        }

        [NotNull]
        public ICommand AddDirectoryCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanAddDirectory, AddDirectory);
            }
        }

        [NotNull]
        public ICommand ClearMappingCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanClearMapping, ClearMapping);
            }
        }

        public WixDirectoryNode MappedNodeSetter
        {
            get
            {
                Contract.Ensures(Contract.Result<WixDirectoryNode>() == null);

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

        public WixDirectoryNode MappedNode
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

        private void MappedNode_Changed(WixDirectoryNode oldValue, WixDirectoryNode newValue)
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
            MappedNode = _wixProject.AddDirectoryNode(_directory);
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_wixProject != null);
            Contract.Invariant(_unmappedNodes != null);
            Contract.Invariant(_directory != null);
        }
    }
}