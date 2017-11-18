namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Desktop;

    public class WixFeatureNode : WixNode
    {
        public WixFeatureNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node) 
            : base(sourceFile, node)
        {
        }

        [NotNull]
        public IEnumerable<string> ComponentGroupRefs => Node
            .Descendants(WixNames.ComponentGroupRefNode)
            // ReSharper disable once AssignNullToNotNullAttribute
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id));

        public void AddComponentGroupRef([NotNull] string id)
        {
            Node.AddWithFormatting(new XElement(WixNames.ComponentGroupRefNode, new XAttribute("Id", id)));
        }
    }
}
