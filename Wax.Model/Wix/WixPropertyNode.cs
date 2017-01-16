namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    public class WixProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class WixPropertyNode : WixNode
    {
        public WixPropertyNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        public WixProperty Property
        {
            get
            {
                return new WixProperty { Name = this.Name, Value = this.GetAttribute("Value") };
            }
        }

        public void AddRegistrySearch(WixRegistrySearch registrySearch)
        {
            var newNode = new XElement(WixNames.RegistrySearch,
                new XAttribute("Id", registrySearch.Id),
                new XAttribute("Root", registrySearch.Root.ToString()),
                new XAttribute("Key", registrySearch.Key),
                new XAttribute("Name", registrySearch.Name),
                new XAttribute("Root", registrySearch.Type.ToString()),
                new XAttribute("Win64", registrySearch.Win64 ? "yes" : "no")
            );
            Node.AddWithFormatting(newNode);
            SourceFile.Save();
        }

        public void Remove()
        {
            Node.RemoveSelfAndWhiteSpace();

            SourceFile.Save();
        }
    }
}
