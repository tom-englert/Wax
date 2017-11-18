namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Mapping;

    public class WixComponentGroupNode : WixNode
    {
        public WixComponentGroupNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        public string Directory => GetAttribute("Directory");

        [NotNull]
        public WixFileNode AddFileComponent([NotNull] string id, [NotNull] string name, [NotNull] FileMapping fileMapping)
        {
            return SourceFile.AddFileComponent(this, id, name, fileMapping);
        }
    }
}