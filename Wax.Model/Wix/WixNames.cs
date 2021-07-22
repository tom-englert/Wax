namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    public class WixNames
    {
        private readonly string _namespace;

        public WixNames(string @namespace)
        {
            _namespace = @namespace;
        }

        public XName FileNode => XName.Get("File", _namespace);

        public XName DirectoryNode => XName.Get("Directory", _namespace);

        public XName FragmentNode => XName.Get("Fragment", _namespace);

        public XName DirectoryRefNode => XName.Get("DirectoryRef", _namespace);

        public XName ComponentNode => XName.Get("Component", _namespace);

        public XName ComponentGroupNode => XName.Get("ComponentGroup", _namespace);

        public XName ComponentRefNode => XName.Get("ComponentRef", _namespace);

        public XName ComponentGroupRefNode => XName.Get("ComponentGroupRef", _namespace);

        public XName FeatureNode => XName.Get("Feature", _namespace);

        public XName FeatureRefNode => XName.Get("FeatureRef", _namespace);

        public XName PropertyNode => XName.Get("Property", _namespace);

        public XName RegistrySearch => XName.Get("RegistrySearch", _namespace);

        public XName CustomActionRefNode => XName.Get("CustomActionRef", _namespace);

        public const string Define = "define";
    }
}
