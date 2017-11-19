namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;

    [Flags]
    public enum BuildFileGroups
    {
        None = 0,
        LocalizedResourceDlls = 1,
        XmlSerializer = 2,
        ContentFiles = 4,
        Built = 8,
        SourceFiles = 16,
        Symbols = 32,
        Documentation = 64
    }
}