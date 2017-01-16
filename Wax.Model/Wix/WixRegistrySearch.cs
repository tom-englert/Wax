namespace tomenglertde.Wax.Model.Wix
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// http://wixtoolset.org/documentation/manual/v3/xsd/wix/registrysearch.html
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegistrySearchRootType
    {
        HKMU,
        HKCR,
        HKCU,
        HKLM
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegistrySearchType
    {
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "directory")]
        directory,
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "file")]
        file,
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "raw")]
        raw,
        DWORD,
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "REG")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "BINARY")]
        REG_BINARY,
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "REG")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EXPAND")]
        REG_EXPAND_SZ,
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "REG")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "MULTI")]
        REG_MULTI_SZ,
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "REG")]
        REG_SZ
    }

    public class WixRegistrySearch
    {
        public string Id { get; set; }
        public RegistrySearchRootType Root { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Win64 { get; set; }
        public RegistrySearchType SearchType { get; set; }
    }
}
