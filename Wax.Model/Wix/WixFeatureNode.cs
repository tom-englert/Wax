namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class WixFeatureNode : WixNode
    {
        public WixFeatureNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node) 
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        [NotNull]
        public IEnumerable<string> ComponentGroupRefs
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return Node
                    .Descendants(WixNames.ComponentGroupRefNode)
                    .Select(node => node.GetAttribute("Id"))
                    .Where(id => !string.IsNullOrEmpty(id));
            }
        }

        public void AddComponentGroupRef([NotNull] string id)
        {
            Contract.Requires(id != null);

            var newNode = new XElement(WixNames.ComponentGroupRefNode);
            newNode.Add(new XAttribute("Id", id));
            Node.Add(newNode);
        }
    }
}
