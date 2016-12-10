// Guids.cs
// MUST match guids.h

namespace tomenglertde.Wax
{
    using System;

    static class GuidList
    {
        public const string guidWaxPkgString = "6b9b0621-c739-4ee5-9834-a0ba2a8d3596";
        public const string guidWaxCmdSetString = "b2efcdeb-6b97-430e-82eb-ef8c27280004";
        public const string guidToolWindowPersistanceString = "ba4ab97f-d341-4b14-b8c9-3cba5e401a5f";

        public static readonly Guid guidWaxCmdSet = new Guid(guidWaxCmdSetString);
    };
}