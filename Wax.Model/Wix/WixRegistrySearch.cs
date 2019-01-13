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
        directory,
        file,
        raw,
        DWORD,
        REG_BINARY,
        REG_EXPAND_SZ,
        REG_MULTI_SZ,
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
