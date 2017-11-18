namespace tomenglertde.Wax.Model.Wix
{
    using System.Linq;
    using System.Xml.Linq;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public class WixDefine
    {
        public WixDefine([NotNull] WixSourceFile sourceFile, [NotNull] XProcessingInstruction node)
        {
            SourceFile = sourceFile;
            Node = node;
        }

        [NotNull]
        // ReSharper disable once PossibleNullReferenceException
        public string Name => Node.Data.Split('=').Select(item => item.Trim()).FirstOrDefault() ?? "<invalid>";

        [Equals]
        [NotNull]
        public XProcessingInstruction Node { get; }

        [NotNull]
        public WixSourceFile SourceFile { get; }
    }
}
