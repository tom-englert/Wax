namespace tomenglertde.Wax.Model.Wix
{
    using System.Linq;
    using System.Xml.Linq;

    using Equatable;

    [ImplementsEquatable]
    public class WixDefine
    {
        public WixDefine(WixSourceFile sourceFile, XProcessingInstruction node)
        {
            SourceFile = sourceFile;
            Node = node;
        }

        public string Name => Node.Data.Split('=').Select(item => item.Trim()).FirstOrDefault() ?? "<invalid>";

        [Equals]
        public XProcessingInstruction Node { get; }

        public WixSourceFile SourceFile { get; }
    }
}
