namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Essentials;

    public class WixComponentNode : WixNode
    {
        public WixComponentNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        public string? Directory => GetAttribute("Directory");

        [NotNull]
        public IEnumerable<string> Files => Node
            .Descendants(WixNames.FileNode)
            .Where(node => node.Parent == Node)
            .Select(node => node.GetAttribute("Id"))
            .Where(id => !string.IsNullOrEmpty(id))
            .ExceptNullItems();

        [NotNull]
        public IEnumerable<WixFileNode> EnumerateFiles(IDictionary<string, WixFileNode> fileNodes)
        {
            return Files.Select(fileNodes.GetValueOrDefault!).ExceptNullItems();
        }
    }
}
