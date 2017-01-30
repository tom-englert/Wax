using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("tom-englert.de")]
[assembly: AssemblyProduct("Wax - The WiX Setup Editor")]
[assembly: AssemblyCopyright("Copyright © tom-englert.de 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyVersion(Product.Version)]
[assembly: AssemblyFileVersion(Product.Version)]

internal static class Product
{
    public const string Version = "1.0.21.0";
}
