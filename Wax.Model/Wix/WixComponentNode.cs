namespace tomenglertde.Wax.Model.Wix
{
    using System.Xml.Linq;

    using JetBrains.Annotations;

    public class WixComponentNode : WixNode
    {
        public WixComponentNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
        }

        [CanBeNull]
        public string Directory => GetAttribute("Directory");
    }
}
