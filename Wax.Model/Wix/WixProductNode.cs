using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using tomenglertde.Wax.Model.Tools;
using TomsToolbox.Desktop;

namespace tomenglertde.Wax.Model.Wix
{
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
                return Node.Descendants(WixNames.PropertyNode).Select(node => new WixPropertyNode(SourceFile,node));
            }
        }

        public WixPropertyNode AddProperty([NotNull] WixProperty property)
        {
            Contract.Requires(property != null);
            Contract.Requires(property.Name != null);
            var newNode = new XElement(WixNames.PropertyNode, new XAttribute("Id", property.Name), new XAttribute("Value", property.Value));
            Node.AddWithFormatting(newNode);
            SourceFile.Save();
            return PropertyNodes.First(pn => pn.Id == property.Name);
        }
    }
}
