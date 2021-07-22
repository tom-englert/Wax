namespace tomenglertde.Wax.Model.Mapping
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using tomenglertde.Wax.Model.Wix;

    using TomsToolbox.Wpf;

    public class UnmappedFile
    {
        private readonly IList<UnmappedFile> _allUnmappedFiles;

        public UnmappedFile(WixFileNode node, IList<UnmappedFile> allUnmappedFiles)
        {
            Node = node;
            _allUnmappedFiles = allUnmappedFiles;
        }

        public ICommand DeleteCommand => new DelegateCommand(Delete);

        public WixFileNode Node { get; }

        public WixFileNode ToWixFileNode()
        {
            return Node;
        }

        public static implicit operator WixFileNode?(UnmappedFile? file) => file?.Node;

        private void Delete()
        {
            _allUnmappedFiles.Remove(this);
            Node.Remove();
        }
    }
}
