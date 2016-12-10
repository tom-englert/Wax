namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using tomenglertde.Wax.Model.Mapping;

    public class WixComponentGroupNode : WixNode
    {
        public WixComponentGroupNode(WixSourceFile sourceFile, XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        public string Directory
        {
            get
            {
                return GetAttribute("Directory");
            }
        }

        public WixFileNode AddFileComponent(string id, string name, FileMapping fileMapping)
        {
            Contract.Requires(id != null);
            Contract.Requires(name != null);
            Contract.Requires(fileMapping != null);
            Contract.Ensures(Contract.Result<WixFileNode>() != null);

            return SourceFile.AddFileComponent(this, id, name, fileMapping);
        }
    }
}