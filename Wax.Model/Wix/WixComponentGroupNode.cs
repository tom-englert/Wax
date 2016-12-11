namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Mapping;

    public class WixComponentGroupNode : WixNode
    {
        public WixComponentGroupNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        public string Directory => GetAttribute("Directory");

        [NotNull]
        public WixFileNode AddFileComponent([NotNull] string id, [NotNull] string name, [NotNull] FileMapping fileMapping)
        {
            Contract.Requires(id != null);
            Contract.Requires(name != null);
            Contract.Requires(fileMapping != null);
            Contract.Ensures(Contract.Result<WixFileNode>() != null);

            return SourceFile.AddFileComponent(this, id, name, fileMapping);
        }
    }
}