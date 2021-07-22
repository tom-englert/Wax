namespace tomenglertde.Wax.Model.Wix
{
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Essentials;

    public class WixDirectoryNode : WixNode
    {
        private WixDirectoryNode? _parent;

        public WixDirectoryNode(WixSourceFile sourceFile, XElement node)
            : base(sourceFile, node)
        {
        }

        public WixDirectoryNode? Parent => _parent ??= ResolveParent();

        public string Path
        {
            get
            {
                var name = Name ?? ".";
                return Parent != null ? (Parent.Path + @"\" + name) : name;
            }
        }

        public WixDirectoryNode AddSubDirectory(string id, string name)
        {
            var directoryElement = new XElement(WixNames.DirectoryNode, new XAttribute("Id", id), new XAttribute("Name", name));

            Node.AddWithFormatting(directoryElement);

            SourceFile.Save();

            return SourceFile.AddDirectoryNode(directoryElement);
        }

        private WixDirectoryNode? ResolveParent()
        {
            var parentElement = Node.Parent;

            if (parentElement == null)
                return null;

            return parentElement.Name.LocalName switch
            {
                "Directory" or "DirectoryRef" => SourceFile.Project.DirectoryNodes.FirstOrDefault(node => node.Id == parentElement.GetAttribute("Id")),
                _ => null,
            };
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Id, Path);
        }
    }
}