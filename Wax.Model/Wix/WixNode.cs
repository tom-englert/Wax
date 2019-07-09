namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    [ImplementsEquatable]
    public class WixNode
    {
        public WixNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
        {
            SourceFile = sourceFile;
            Node = node;
        }

        [Equals]
        [NotNull, UsedImplicitly]
        public string Kind => Node.Name.LocalName;

        [Equals]
        [NotNull]
        public string Id => GetAttribute("Id") ?? string.Empty;

        [CanBeNull]
        public string Name => GetAttribute("Name");

        [NotNull]
        internal XElement Node { get; }

        [NotNull]
        public WixSourceFile SourceFile { get; }

        [NotNull]
        public WixNames WixNames => SourceFile.WixNames;

        [CanBeNull]
        protected string GetAttribute([NotNull] string name)
        {
            return Node.GetAttribute(name);
        }
    }
}