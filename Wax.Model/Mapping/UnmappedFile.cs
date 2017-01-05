namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Wpf;

    public class UnmappedFile
    {
        [NotNull]
        private readonly WixFileNode _node;
        [NotNull]
        private readonly IList<UnmappedFile> _allUnmappedFiles;

        public UnmappedFile([NotNull] WixFileNode node, [NotNull] IList<UnmappedFile> allUnmappedFiles)
        {
            Contract.Requires(node != null);
            Contract.Requires(allUnmappedFiles != null);

            _node = node;
            _allUnmappedFiles = allUnmappedFiles;
        }

        [NotNull]
        public ICommand DeleteCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(Delete);
            }
        }

        [NotNull]
        public WixFileNode Node
        {
            get
            {
                Contract.Ensures(Contract.Result<WixFileNode>() != null);

                return _node;
            }
        }

        [NotNull]
        public WixFileNode ToWixFileNode()
        {
            Contract.Ensures(Contract.Result<WixFileNode>() != null);

            return _node;
        }

        public static implicit operator WixFileNode(UnmappedFile file)
        {
            return file == null ? null : file.Node;
        }

        private void Delete()
        {
            _allUnmappedFiles.Remove(this);
            _node.Remove();
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_allUnmappedFiles != null);
            Contract.Invariant(_node != null);
        }
    }
}
