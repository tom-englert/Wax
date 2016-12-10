namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class WixDirectoryNode : WixNode
    {
        private WixDirectoryNode _parent;

        public WixDirectoryNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        public WixDirectoryNode Parent
        {
            get
            {
                return _parent ?? (_parent = ResolveParent());
            }
        }

        public string Path
        {
            get
            {
                return Parent != null ? (Parent.Path + @"\" + Name) : Name;
            }
        }

        [ContractVerification(false)] // because of switch(string) 
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

        [NotNull]
        public WixDirectoryNode AddDirectory([NotNull] string id, [NotNull] string name)
        {
            Contract.Requires(id != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<WixDirectoryNode>() != null);

            var directoryElement = new XElement(WixNames.DirectoryNode);
            directoryElement.Add(new XAttribute("Id", id));
            directoryElement.Add(new XAttribute("Name", name));
            Node.Add(directoryElement);

            SourceFile.Save();

            return SourceFile.AddDirectoryNode(directoryElement);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Id, Path);
        }
    }
}