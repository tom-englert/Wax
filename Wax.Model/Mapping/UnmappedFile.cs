namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Wpf;

    public class UnmappedFile
    {
        [NotNull]
        private readonly IList<UnmappedFile> _allUnmappedFiles;

        public UnmappedFile([NotNull] WixFileNode node, [NotNull] IList<UnmappedFile> allUnmappedFiles)
        {
            Node = node;
            _allUnmappedFiles = allUnmappedFiles;
        }

        [NotNull]
        public ICommand DeleteCommand => new DelegateCommand(Delete);

        [NotNull]
        public WixFileNode Node { get; }

        [NotNull]
        public WixFileNode ToWixFileNode()
        {
            return Node;
        }

        [CanBeNull]
        public static implicit operator WixFileNode([CanBeNull] UnmappedFile file) => file?.Node;

        private void Delete()
        {
            _allUnmappedFiles.Remove(this);
            Node.Remove();
        }
    }
}
