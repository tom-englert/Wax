namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Essentials;

    [ImplementsEquatable]
    public class WixNode
    {
        public WixNode(WixSourceFile sourceFile, XElement node)
        {
            SourceFile = sourceFile;
            Node = node;
        }

        [Equals]
        [UsedImplicitly]
        public string Kind => Node.Name.LocalName;

        [Equals]
        public string Id => GetAttribute("Id") ?? string.Empty;

        public string? Name => GetAttribute("Name");

        internal XElement Node { get; }

        public WixSourceFile SourceFile { get; }

        public WixNames WixNames => SourceFile.WixNames;

        protected string? GetAttribute(string name)
        {
            return Node.GetAttribute(name);
        }
    }
}