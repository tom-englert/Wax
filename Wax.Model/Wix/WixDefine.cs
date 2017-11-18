namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public class WixDefine
    {
        [NotNull]
        private readonly WixSourceFile _sourceFile;
        [NotNull]
        private readonly XProcessingInstruction _node;

        public WixDefine([NotNull] WixSourceFile sourceFile, [NotNull] XProcessingInstruction node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);

            _sourceFile = sourceFile;
            _node = node;
        }

        [NotNull]
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _node.Data.Split('=').Select(item => item.Trim()).FirstOrDefault() ?? "<invalid>";
            }
        }

        [Equals]
        [NotNull]
        public XProcessingInstruction Node
        {
            get
            {
                Contract.Ensures(Contract.Result<XProcessingInstruction>() != null);

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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_sourceFile != null);
            Contract.Invariant(_node != null);
        }
    }
}
