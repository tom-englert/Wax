namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Essentials;

    public class WixFeatureNode : WixNode
    {
        public WixFeatureNode(WixSourceFile sourceFile, XElement node)
            : base(sourceFile, node)
        {
        }

        public WixFeatureNode? Parent { get; private set; }

        public IEnumerable<string> ComponentGroupRefs => Node
            .Descendants(WixNames.ComponentGroupRefNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();
        
        public IEnumerable<string> ComponentRefs => Node
            .Descendants(WixNames.ComponentRefNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        public IEnumerable<string> Components => Node
            .Descendants(WixNames.ComponentNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        public IEnumerable<string> Features => Node
            .Descendants(WixNames.FeatureNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        public IEnumerable<string> FeatureRefs => Node
            .Descendants(WixNames.FeatureRefNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        public void AddComponentGroupRef(string id)
        {
            Node.AddWithFormatting(new XElement(WixNames.ComponentGroupRefNode, new XAttribute("Id", id)));
        }

        public void BuildTree(IDictionary<string, WixFeatureNode> allFeatures)
        {
            var children = Features.Concat(FeatureRefs)
                .Select(allFeatures.GetValueOrDefault!)
                .ExceptNullItems();

            foreach (var child in children)
            {
                child.Parent = this;
            }
        }

        public IEnumerable<WixFileNode> EnumerateInstalledFiles(IDictionary<string, WixComponentGroupNode> componentGroupNodes, IDictionary<string, WixComponentNode> componentNodes, IDictionary<string, WixFileNode> fileNodes)
        {
            var byComponentGroupRef = ComponentGroupRefs.Select(componentGroupNodes.GetValueOrDefault)
                .ExceptNullItems()
                .SelectMany(cg => cg.EnumerateComponents(componentGroupNodes, componentNodes));

            var byComponentRef = ComponentRefs.Select(componentNodes.GetValueOrDefault)
                .ExceptNullItems();

            var children = Components.Select(componentNodes.GetValueOrDefault)
                .ExceptNullItems();

            var files = byComponentGroupRef.Concat(byComponentRef).Concat(children).SelectMany(c => c?.EnumerateFiles(fileNodes));

            if (Parent != null)
            {
                files = Parent.EnumerateInstalledFiles(componentGroupNodes, componentNodes, fileNodes).Concat(files);
            }

            return files;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
