namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    public class WixProperty
    {
        [NotNull]
        private readonly string _name;
        [NotNull]
        private string _value;

        public WixProperty([NotNull] string name, [NotNull] string value)
        {
            Contract.Requires(name != null);
            Contract.Requires(value != null);

            _name = name;
            _value = value;
        }

        [NotNull]
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name;
            }
        }

        [NotNull]
        public string Value
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _value;
            }
            set
            {
                Contract.Requires(value != null);
                _value = value;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_name != null);
            Contract.Invariant(_value != null);
        }

    }

    public class WixPropertyNode : WixNode
    {
        public WixPropertyNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        [NotNull]
        public WixProperty Property
        {
            get
            {
                Contract.Ensures(Contract.Result<WixProperty>() != null);

                return new WixProperty(Id, GetAttribute("Value") ?? string.Empty);
            }
        }

        [ContractVerification(false)] // TODO: add contracts to WixRegistrySearch...
        public void AddRegistrySearch([NotNull] WixRegistrySearch registrySearch)
        {
            Contract.Requires(registrySearch != null);

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
