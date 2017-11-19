namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using JetBrains.Annotations;

    public class WixNames
    {
        [NotNull]
        private readonly string _namespace;

        public WixNames([NotNull] string @namespace)
        {
            _namespace = @namespace;
        }

        [NotNull]
        public XName FileNode => XName.Get("File", _namespace);

        [NotNull]
        public XName DirectoryNode => XName.Get("Directory", _namespace);

        [NotNull]
        public XName FragmentNode => XName.Get("Fragment", _namespace);

        [NotNull]
        public XName DirectoryRefNode => XName.Get("DirectoryRef", _namespace);

        [NotNull]
        public XName ComponentNode => XName.Get("Component", _namespace);

        [NotNull]
        public XName ComponentGroupNode => XName.Get("ComponentGroup", _namespace);

        [NotNull]
        public XName ComponentRefNode => XName.Get("ComponentRef", _namespace);

        [NotNull]
        public XName ComponentGroupRefNode => XName.Get("ComponentGroupRef", _namespace);

        [NotNull]
        public XName FeatureNode => XName.Get("Feature", _namespace);

        [NotNull]
        public XName FeatureRefNode => XName.Get("FeatureRef", _namespace);

        [NotNull]
        public XName PropertyNode => XName.Get("Property", _namespace);

        [NotNull]
        public XName RegistrySearch => XName.Get("RegistrySearch", _namespace);

        [NotNull]
        public XName CustomActionRefNode => XName.Get("CustomActionRef", _namespace);

        [NotNull]
        public const string Define = "define";
    }
}
