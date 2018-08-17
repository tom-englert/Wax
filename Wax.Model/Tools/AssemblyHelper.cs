namespace tomenglertde.Wax.Model.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Windows.Baml2006;
    using System.Xaml;

    using JetBrains.Annotations;

    using TomsToolbox.Core;

    public static class AssemblyHelper
    {
        [NotNull] private static readonly Dictionary<string, ReferenceCacheEntry> _referenceCache = new Dictionary<string, ReferenceCacheEntry>(StringComparer.OrdinalIgnoreCase);
        [NotNull] private static readonly Dictionary<string, DirectoryCacheEntry> _directoryCache = new Dictionary<string, DirectoryCacheEntry>(StringComparer.OrdinalIgnoreCase);

        [NotNull]
        public static AssemblyName[] FindReferences([NotNull] string target, [NotNull] string outputDirectory)
        {
            var timeStamp = File.GetLastWriteTime(target);

            if (_referenceCache.TryGetValue(target, out var cacheEntry) && (cacheEntry.TimeStamp == timeStamp))
            {
                return cacheEntry.References;
            }

            var existingAssemblies = FindExistingAssemblies(outputDirectory);

            var references = InvokeInSeparateDomain(target, existingAssemblies);

            _referenceCache[target] = new ReferenceCacheEntry(references, timeStamp);

            return references;
        }

        [NotNull]
        private static Dictionary<string, AssemblyName> FindExistingAssemblies([NotNull] string outputDirectory)
        {
            var folder = new DirectoryInfo(outputDirectory);
            var files = folder.GetFiles("*.dll");
            var hash = files.Select(file => file.LastWriteTime.GetHashCode()).Aggregate(0, HashCode.Aggregate);

            Dictionary<string, AssemblyName> existingAssemblies;

            if (_directoryCache.TryGetValue(folder.FullName, out var directoryCacheEntry) && (directoryCacheEntry.Hash == hash))
            {
                existingAssemblies = directoryCacheEntry.AssemblyNames;
            }
            else
            {
                existingAssemblies = files
                    .Select(file => file.FullName)
                    .Select(TryGetAssemblyName)
                    .Where(assemblyName => assemblyName != null)
                    .ToDictionary(assemblyName => assemblyName.Name);

                _directoryCache[folder.FullName] = new DirectoryCacheEntry(existingAssemblies, hash);
            }

            return existingAssemblies;
        }

        [CanBeNull]
        private static AssemblyName TryGetAssemblyName([NotNull] string assemblyFile)
        {
            try
            {
                return AssemblyName.GetAssemblyName(assemblyFile);
            }
            catch
            {
                return null;
            }
        }

        [CanBeNull]
        private static AssemblyName[] InvokeInSeparateDomain([NotNull] string target, [NotNull] IDictionary<string, AssemblyName> existingAssemblies)
        {
            var friendlyName = "Temporary domain";
            var currentDomain = AppDomain.CurrentDomain;
            var implementingType = typeof(InternalHelper);
            var baseDirectory = Path.GetDirectoryName(implementingType.Assembly.Location);

            var appDomain = AppDomain.CreateDomain(friendlyName, currentDomain.Evidence, baseDirectory, string.Empty, false);

            if (appDomain == null)
                return new AssemblyName[0];

            try
            {
                var assemblyNameList = appDomain.CreateInstanceAndUnwrap(
                    implementingType.Assembly.FullName,
                    implementingType.FullName,
                    true,
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new object[] {target, existingAssemblies},
                    CultureInfo.InvariantCulture,
                    new object[0]);

                var result = ((IEnumerable<AssemblyName>)assemblyNameList).ToArray();

                return result;
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        private class InternalHelper : MarshalByRefObject, IEnumerable<AssemblyName>
        {
            [NotNull] private readonly IList<AssemblyName> _assemblyNames;

            private InternalHelper([NotNull] string target, [NotNull] IDictionary<string, AssemblyName> existingAssemblies)
            {
                _assemblyNames = FindReferences(target, existingAssemblies);
            }

            [NotNull]
            private static AssemblyName[] FindReferences([NotNull] string target, [NotNull] IDictionary<string, AssemblyName> existingAssemblies)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(target);

                    var usedAssemblies = FindXamlRefrences(existingAssemblies, assembly);

                    var referencedAssemblyNames = assembly.GetReferencedAssemblies()
                        .Select(item => item?.Name)
                        .Where(item => item != null)
                        .Select(existingAssemblies.GetValueOrDefault)
                        .Where(item => item != null);

                    usedAssemblies.AddRange(referencedAssemblyNames);

                    return usedAssemblies.ToArray();
                }
                catch (BadImageFormatException)
                {
                    return new AssemblyName[0];
                }
            }

            [NotNull]
            private static HashSet<AssemblyName> FindXamlRefrences([NotNull] IDictionary<string, AssemblyName> existingAssemblies, [NotNull] Assembly assembly)
            {
                var assembyResources = assembly.GetManifestResourceNames().FirstOrDefault(res => res.EndsWith("g.resources"));

                var usedAssemblies = new HashSet<AssemblyName>();

                if (assembyResources == null)
                    return usedAssemblies;

                Assembly AssemblyResolve(object sender, ResolveEventArgs args)
                {
                    var requestedAssemblyName = new AssemblyName(args.Name);

                    if (existingAssemblies.TryGetValue(requestedAssemblyName.Name, out var assemblyName) && (requestedAssemblyName.Version <= assemblyName.Version))
                    {
                        usedAssemblies.Add(assemblyName);

                        return Assembly.Load(assemblyName);
                    }

                    return null;
                }

                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

                using (var resourceStream = assembly.GetManifestResourceStream(assembyResources))
                {
                    using (var resourceReader = new ResourceReader(resourceStream))
                    {
                        foreach (DictionaryEntry entry in resourceReader)
                        {
                            if ((entry.Key as string)?.EndsWith(".baml") == true)
                            {
                                using (var bamlStream = (Stream) entry.Value)
                                {
                                    using (var bamlReader = new Baml2006Reader(bamlStream, new XamlReaderSettings {ProvideLineInfo = true}))
                                    {
                                        try
                                        {
                                            while (bamlReader.Read())
                                            {
                                                if (bamlReader.NodeType == XamlNodeType.StartMember)
                                                {
                                                    try
                                                    {
                                                        var type = bamlReader.Member.DeclaringType?.UnderlyingType;
                                                        if (type != null)
                                                        {
                                                            var requestedAssemblyName = type.Assembly.GetName();

                                                            if (existingAssemblies.TryGetValue(requestedAssemblyName.Name, out var assemblyName) && (requestedAssemblyName.Version <= assemblyName.Version))
                                                            {
                                                                usedAssemblies.Add(assemblyName);
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // nothing left to do...
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return usedAssemblies;
            }

            public IEnumerator<AssemblyName> GetEnumerator()
            {
                return _assemblyNames.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReferenceCacheEntry
        {
            public ReferenceCacheEntry(AssemblyName[] references, DateTime timeStamp)
            {
                References = references;
                TimeStamp = timeStamp;
            }

            public AssemblyName[] References { get; }

            public DateTime TimeStamp { get; }
        }

        private class DirectoryCacheEntry
        {
            public DirectoryCacheEntry([NotNull] Dictionary<string, AssemblyName> assemblyNames, int hash)
            {
                AssemblyNames = assemblyNames;
                Hash = hash;
            }

            [NotNull]
            public Dictionary<string, AssemblyName> AssemblyNames { get; }

            public int Hash { get; }
        }
    }
}
