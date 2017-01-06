﻿namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    public class WixDefine : IEquatable<WixDefine>
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

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _node.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WixDefine);
        }

        /// <summary>
        /// Determines whether the specified <see cref="WixDefine"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="WixDefine"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="WixDefine"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(WixDefine other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(WixDefine left, WixDefine right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return Equals(left._node, right._node);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(WixDefine left, WixDefine right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(WixDefine left, WixDefine right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_sourceFile != null);
            Contract.Invariant(_node != null);
        }
    }
}
