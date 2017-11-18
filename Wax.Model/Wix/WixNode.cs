namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    [ImplementsEquatable]
    public class WixNode
    {
        [NotNull]
        private readonly WixSourceFile _sourceFile;
        [NotNull]
        private readonly XElement _node;

        public WixNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);

            _sourceFile = sourceFile;
            _node = node;
        }

        [Equals]
        [NotNull]
        public string Kind
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Node.Name.LocalName;
            }
        }

        [Equals]
        [NotNull]
        public string Id
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return GetAttribute("Id") ?? string.Empty;
            }
        }

        [CanBeNull]
        public string Name => GetAttribute("Name");

        [NotNull]
        internal XElement Node
        {
            get
            {
                Contract.Ensures(Contract.Result<XElement>() != null);

                return _node;
            }
        }

        [NotNull]
        public WixSourceFile SourceFile
        {
            get
            {
                Contract.Ensures(Contract.Result<WixSourceFile>() != null);

                return _sourceFile;
            }
        }

        [NotNull]
        public WixNames WixNames
        {
            get
            {
                Contract.Ensures(Contract.Result<WixNames>() != null);

                return SourceFile.WixNames;
            }
        }

        [CanBeNull]
        protected string GetAttribute([NotNull] string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));

            return Node.GetAttribute(name);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_node != null);
            Contract.Invariant(_sourceFile != null);
        }
    }
}