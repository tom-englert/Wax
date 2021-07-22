﻿namespace tomenglertde.Wax.Model.Tools
{
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public static class XmlExtensions
    {
        public static void RemoveSelfAndWhiteSpace(this XElement element)
        {
            if ((element.PreviousNode is XText previous) && string.IsNullOrWhiteSpace(previous.Value))
            {
                previous.Remove();
            }

            element.Remove();
        }

        public static void AddWithFormatting(this XElement parent, XElement item)
        {
            var firstNode = parent.FirstNode;
            var lastNode = parent.LastNode;

            if ((firstNode?.NodeType == XmlNodeType.Text) && (lastNode != null))
            {
                var whiteSpace = "\n" + ((firstNode as XText)?.Value?.Split('\n').LastOrDefault() ?? new string(' ', lastNode.GetDefaultIndent()));

                lastNode.AddBeforeSelf(new XText(whiteSpace), item);
            }
            else
            {
                var previousNode = parent.PreviousNode;

                var whiteSpace = "\n" + ((previousNode as XText)?.Value?.Split('\n').LastOrDefault() ?? new string(' ', parent.GetDefaultIndent()));

                parent.Add(new XText(whiteSpace + "  "), item, new XText(whiteSpace));
            }
        }

        private static int GetDefaultIndent(this XObject item)
        {
            return item.Parent?.GetDefaultIndent() ?? 0 + 2;
        }
    }
}
