namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Desktop;

    public class WixProductNode : WixNode
    {
        public WixProductNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        [NotNull, ItemNotNull]
        public IEnumerable<WixPropertyNode> PropertyNodes => Node.Descendants(WixNames.PropertyNode).Select(node => new WixPropertyNode(SourceFile, node));

        [NotNull, ItemNotNull]
        public IEnumerable<string> EnumerateCustomActionRefs()
        {
            return Node.Descendants(WixNames.CustomActionRefNode).Select(node => node.GetAttribute("Id")).Where(item => item != null);
        }

        [NotNull]
        public WixPropertyNode AddProperty([NotNull] WixProperty property)
        {
            var newNode = new XElement(WixNames.PropertyNode, new XAttribute("Id", property.Name), new XAttribute("Value", property.Value));

            Node.AddWithFormatting(newNode);

            SourceFile.Save();

            return new WixPropertyNode(SourceFile, newNode);
        }

        public void AddCustomActionRef([NotNull] string id)
        {
            var newNode = new XElement(WixNames.CustomActionRefNode, new XAttribute("Id", id));

            Node.AddWithFormatting(newNode);

            SourceFile.Save();
        }
    }
}
