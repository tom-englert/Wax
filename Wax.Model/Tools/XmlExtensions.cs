using JetBrains.Annotations;

namespace tomenglertde.Wax.Model.Tools
{
    using System.Diagnostics.Contracts;
    using System.Xml;
    using System.Xml.Linq;

    public static class XmlExtensions
    {
        public static void RemoveSelfAndWhiteSpace([NotNull] this XElement element)
        {
            Contract.Requires(element != null);

            var previous = element.PreviousNode as XText;

            if ((previous != null) && string.IsNullOrWhiteSpace(previous.Value))
            {
                previous.Remove();
            }

            element.Remove();
        }

        public static void AddWithFormatting([NotNull] this XElement parent, [NotNull] XElement item)
        {
            Contract.Requires(parent != null);
            Contract.Requires(item != null);

            var lastNode = parent.LastNode;

            if (lastNode?.NodeType == XmlNodeType.Text)
            {
                var lastDelimiter = lastNode.PreviousNode?.PreviousNode as XText;
                var whiteSpace = new XText(lastDelimiter?.Value ?? "\n    ");
                lastNode.AddBeforeSelf(whiteSpace, item);
            }
            else
            {
                var previousNode = parent.PreviousNode;

                var whiteSpace = (previousNode as XText)?.Value ?? "\n  ";

                parent.Add(new XText(whiteSpace + "  "), item, new XText(whiteSpace));
            }
        }
    }
}
