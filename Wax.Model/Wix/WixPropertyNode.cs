namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    public class WixProperty
    {
        public WixProperty([NotNull] string name, [NotNull] string value)
        {
            Name = name;
            Value = value;
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public string Value { get; }
    }

    public class WixPropertyNode : WixNode
    {
        public WixPropertyNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        [NotNull]
        public WixProperty Property => new WixProperty(Id, GetAttribute("Value") ?? string.Empty);

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void AddRegistrySearch([NotNull] WixRegistrySearch registrySearch)
        {
            var newNode = new XElement(WixNames.RegistrySearch,
                new XAttribute("Id", registrySearch.Id),
                new XAttribute("Root", registrySearch.Root.ToString()),
                new XAttribute("Key", registrySearch.Key),
                new XAttribute("Name", registrySearch.Name),
                new XAttribute("Type", registrySearch.SearchType.ToString()),
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
