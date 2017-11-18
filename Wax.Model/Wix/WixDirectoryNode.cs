namespace tomenglertde.Wax.Model.Wix
{
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Desktop;

    public class WixDirectoryNode : WixNode
    {
        private WixDirectoryNode _parent;

        public WixDirectoryNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        [CanBeNull]
        public WixDirectoryNode Parent => _parent ?? (_parent = ResolveParent());

        [NotNull]
        public string Path
        {
            get
            {
                var name = Name ?? ".";
                return Parent != null ? (Parent.Path + @"\" + name) : name;
            }
        }

        [NotNull]
        public WixDirectoryNode AddSubdirectory([NotNull] string id, [NotNull] string name)
        {
            var directoryElement = new XElement(WixNames.DirectoryNode, new XAttribute("Id", id), new XAttribute("Name", name));

            Node.AddWithFormatting(directoryElement);

            SourceFile.Save();

            return SourceFile.AddDirectoryNode(directoryElement);
        }

        [CanBeNull]
        private WixDirectoryNode ResolveParent()
        {
            var parentElement = Node.Parent;

            if (parentElement == null)
                return null;

            switch (parentElement.Name.LocalName)
            {
                case "Directory":
                case "DirectoryRef":
                    return SourceFile.Project.DirectoryNodes.FirstOrDefault(node => node.Id == parentElement.GetAttribute("Id"));
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Id, Path);
        }
    }
}