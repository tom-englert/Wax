namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    public static class WixNames
    {
        public const string Namespace = "http://schemas.microsoft.com/wix/2006/wi";
        public static readonly XName FileNode = XName.Get("File", Namespace);
        public static readonly XName DirectoryNode = XName.Get("Directory", Namespace);
        public static readonly XName FragmentNode = XName.Get("Fragment", Namespace);
        public static readonly XName DirectoryRefNode = XName.Get("DirectoryRef", Namespace);
        public static readonly XName ComponentNode = XName.Get("Component", Namespace);
        public static readonly XName ComponentGroupNode = XName.Get("ComponentGroup", Namespace);
        public static readonly XName ComponentGroupRefNode = XName.Get("ComponentGroupRef", Namespace);
        public static readonly XName FeatureNode = XName.Get("Feature", Namespace);

        public const string Define = "define";
    }
}
