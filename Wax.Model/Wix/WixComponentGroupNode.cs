namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Mapping;

    using TomsToolbox.Essentials;

    public class WixComponentGroupNode : WixNode
    {
        public WixComponentGroupNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        public string? Directory => GetAttribute("Directory");

        [NotNull]
        public IEnumerable<string> Components => Node
            .Descendants(WixNames.ComponentNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        [NotNull]
        public IEnumerable<string> ComponentRefs => Node
            .Descendants(WixNames.ComponentRefNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        [NotNull]
        public IEnumerable<string> ComponentGroupRefs => Node
            .Descendants(WixNames.ComponentGroupRefNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        [NotNull]
        public WixFileNode AddFileComponent([NotNull] string id, [NotNull] string name, [NotNull] FileMapping fileMapping)
        {
            return SourceFile.AddFileComponent(this, id, name, fileMapping);
        }

        [NotNull]
        public IEnumerable<WixComponentNode> EnumerateComponents([NotNull] IDictionary<string, WixComponentGroupNode> componentGroupNodes, [NotNull] IDictionary<string, WixComponentNode> componentNodes)
        {
            var byComponentGroupRef = ComponentGroupRefs.Select(componentGroupNodes.GetValueOrDefault)
                .ExceptNullItems()
                .SelectMany(cg => cg.EnumerateComponents(componentGroupNodes, componentNodes));

            var byComponentRef = ComponentRefs.Select(componentNodes.GetValueOrDefault)
                .ExceptNullItems();

            var children = Components.Select(componentNodes.GetValueOrDefault)
                .ExceptNullItems();


            return byComponentGroupRef.Concat(byComponentRef).Concat(children);
        }
    }
}