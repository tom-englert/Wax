namespace tomenglertde.Wax.Model.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;

    using Baml;

    using Mono.Cecil;

    using TomsToolbox.Essentials;

    public static class AssemblyHelper
    {
        private static readonly Dictionary<string, ReferenceCacheEntry> _referenceCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DirectoryCacheEntry> _directoryCache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<AssemblyName> FindReferences(string target, string outputDirectory)
        {
            var timeStamp = File.GetLastWriteTime(target);

            if (_referenceCache.TryGetValue(target, out var cacheEntry) && (cacheEntry.TimeStamp == timeStamp))
            {
                return cacheEntry.References;
            }

            var existingAssemblies = FindExistingAssemblies(outputDirectory);

            var references = FindReferences(target, existingAssemblies);

            _referenceCache[target] = new ReferenceCacheEntry(references, timeStamp);

            return references;
        }

        private static Dictionary<string, AssemblyName> FindExistingAssemblies(string outputDirectory)
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
                    .ExceptNullItems()
                    .ToDictionary(assemblyName => assemblyName.Name);

                _directoryCache[folder.FullName] = new DirectoryCacheEntry(existingAssemblies, hash);
            }

            return existingAssemblies;
        }

        private static AssemblyName? TryGetAssemblyName(string assemblyFile)
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

        private static IReadOnlyCollection<AssemblyName> FindReferences(string target, IDictionary<string, AssemblyName> existingAssemblies)
        {
            try
            {
                var assembly = ModuleDefinition.ReadModule(target);

                var usedAssemblies = FindXamlReferences(existingAssemblies, assembly);

                var referencedAssemblyNames = assembly.AssemblyReferences
    .Select(item => item?.Name)
    .ExceptNullItems()
    .Select(existingAssemblies.GetValueOrDefault)
    .ExceptNullItems();

                usedAssemblies.AddRange(referencedAssemblyNames);

                return usedAssemblies.ToList().AsReadOnly();
            }
            catch
            {
                return Array.Empty<AssemblyName>();
            }
        }

        private static HashSet<AssemblyName> FindXamlReferences(IDictionary<string, AssemblyName> existingAssemblies, ModuleDefinition assembly)
        {
            var assemblyResources = assembly.Resources?.OfType<EmbeddedResource>().FirstOrDefault(res => res.Name?.EndsWith("g.resources", StringComparison.Ordinal) == true);

            var usedAssemblies = new HashSet<AssemblyName>();

            if (assemblyResources == null)
                return usedAssemblies;

            var resourceStream = assemblyResources.GetResourceStream();

            if (resourceStream == null)
                return usedAssemblies;

            using (var resourceReader = new ResourceReader(resourceStream))
            {
                foreach (DictionaryEntry entry in resourceReader)
                {
                    if ((entry.Key as string)?.EndsWith(".baml", StringComparison.Ordinal) != true)
                        continue;

                    if (entry.Value is not Stream bamlStream)
                        continue;

                    var records = Baml.ReadDocument(bamlStream);

                    foreach (var name in records.OfType<AssemblyInfoRecord>().Select(ai => new AssemblyName(ai.AssemblyFullName)))
                    {
                        if (existingAssemblies.TryGetValue(name.Name, out var assemblyName) && ((name.Version == null) || (name.Version <= assemblyName.Version)))
                        {
                            usedAssemblies.Add(assemblyName);
                        }
                    }
                }
            }

            return usedAssemblies;
        }

        private class ReferenceCacheEntry
        {
            public ReferenceCacheEntry(IReadOnlyCollection<AssemblyName> references, DateTime timeStamp)
            {
                References = references;
                TimeStamp = timeStamp;
            }

            public IReadOnlyCollection<AssemblyName> References { get; }

            public DateTime TimeStamp { get; }
        }

        private class DirectoryCacheEntry
        {
            public DirectoryCacheEntry(Dictionary<string, AssemblyName> assemblyNames, int hash)
            {
                AssemblyNames = assemblyNames;
                Hash = hash;
            }

            public Dictionary<string, AssemblyName> AssemblyNames { get; }

            public int Hash { get; }
        }
    }
}
