namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    using TomsToolbox.Desktop;

    public class WixNode : IEquatable<WixNode>
    {
        private readonly WixSourceFile _sourceFile;
        private readonly XElement _node;

        public WixNode(WixSourceFile sourceFile, XElement node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);

            _sourceFile = sourceFile;
            _node = node;
        }

        public string Kind
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Node.Name.LocalName;
            }
        }

        public string Id
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return GetAttribute("Id") ?? string.Empty;
            }
        }

        public string Name
        {
            get
            {
                return GetAttribute("Name");
            }
        }

        internal XElement Node
        {
            get
            {
                Contract.Ensures(Contract.Result<XElement>() != null);

                return _node;
            }
        }

        public WixSourceFile SourceFile
        {
            get
            {
                Contract.Ensures(Contract.Result<WixSourceFile>() != null);

                return _sourceFile;
            }
        }

        public string GetAttribute(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));

            return Node.GetAttribute(name);
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return (Kind + "/" + Id).GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WixNode);
        }

        /// <summary>
        /// Determines whether the specified <see cref="WixNode"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="WixNode"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="WixNode"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(WixNode other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(WixNode left, WixNode right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return (left.Kind == right.Kind) && (left.Id == right.Id);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(WixNode left, WixNode right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(WixNode left, WixNode right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_node != null);
            Contract.Invariant(_sourceFile != null);
        }
    }
}