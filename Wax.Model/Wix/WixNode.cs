namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    [ImplementsEquatable]
    public class WixNode
    {
        [NotNull]
        private readonly WixSourceFile _sourceFile;
        [NotNull]
        private readonly XElement _node;

        public WixNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
        {
            _sourceFile = sourceFile;
            _node = node;
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
        internal XElement Node => _node;

        [NotNull]
        public WixSourceFile SourceFile => _sourceFile;

        [NotNull]
        public WixNames WixNames => SourceFile.WixNames;

        [CanBeNull]
        protected string GetAttribute([NotNull] string name)
        {
            return Node.GetAttribute(name);
        }
    }
}