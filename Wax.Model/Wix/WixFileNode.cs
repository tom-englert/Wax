namespace tomenglertde.Wax.Model.Wix
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class WixFileNode : WixNode
    {
        [NotNull]
        private readonly IList<WixFileNode> _collection;
        private WixComponentGroupNode _componentGroup;

        public WixFileNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node, [NotNull] IList<WixFileNode> collection)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
            Contract.Requires(collection != null);

            _collection = collection;
        }

        public string Source
        {
            get
            {
                return GetAttribute("Source");
            }
        }

        public WixComponentGroupNode ComponentGroup
        {
            get
            {
                return _componentGroup ?? (_componentGroup = ResolveComponentGroup());
            }
        }

        public void Remove()
        {
            var parentNode = Node.Parent;

            _collection.Remove(this);
            Node.Remove();

            if (parentNode != null && (parentNode.Name == WixNames.ComponentNode) && !parentNode.Descendants().Any())
            {
                parentNode.Remove();
            }

            SourceFile.Save();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} ({1}, {2})", Id, Name, Source);
        }

        private WixComponentGroupNode ResolveComponentGroup()
        {
            var componentNode = Node.Parent;

            if (componentNode == null)
                return null;

            var componentGroupNode = componentNode.Parent;

            if (componentGroupNode == null)
                return null;

            var componentGroups = SourceFile.Project.ComponentGroups;

            if (componentGroupNode.Name == WixNames.ComponentGroupNode)
            {
                _componentGroup = componentGroups.FirstOrDefault(group => group.Node == componentGroupNode);
            }
            else if (componentGroupNode.Name == WixNames.ComponentGroupRefNode)
            {
                _componentGroup = componentGroups.FirstOrDefault(group => group.Id == componentGroupNode.GetAttribute("Id"));
            }

            return null;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_collection != null);
        }
    }
}