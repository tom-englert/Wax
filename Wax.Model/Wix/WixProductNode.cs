namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    public class WixProductNode : WixNode
    {
        public WixProductNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        [NotNull]
        public IEnumerable<WixPropertyNode> PropertyNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixPropertyNode>>() != null);

                return Node.Descendants(WixNames.PropertyNode).Select(node => new WixPropertyNode(SourceFile, node));
            }
        }

        [NotNull]
        public IEnumerable<string> EnumerateCustomActionRefs()
        {
            Contract.Ensures(Contract.Result<IEnumerable<WixPropertyNode>>() != null);

            return Node.Descendants(WixNames.CustomActionRefNode).Select(node => node.Attribute("Id").Value);
        }


        public WixPropertyNode AddProperty([NotNull] WixProperty property)
        {
            Contract.Requires(property != null);

            var newNode = new XElement(WixNames.PropertyNode, new XAttribute("Id", property.Name), new XAttribute("Value", property.Value));

            Node.AddWithFormatting(newNode);

            SourceFile.Save();

            return new WixPropertyNode(SourceFile, newNode);
        }

        public void AddCustomActionRef(string id)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(id));

            var newNode = new XElement(WixNames.CustomActionRefNode, new XAttribute("Id", id));

            Node.AddWithFormatting(newNode);

            SourceFile.Save();
        }
    }
}
