namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Lazy;

    using Equatable;

    using JetBrains.Annotations;

    using tomenglertde.Wax.Model.Tools;

    using TomsToolbox.Essentials;

    [ImplementsEquatable]
    public class Project : INotifyPropertyChanged
    {
        private const BuildFileGroups AllDeployGroups = BuildFileGroups.Built | BuildFileGroups.ContentFiles | BuildFileGroups.LocalizedResourceDlls | BuildFileGroups.Symbols;

        private readonly EnvDTE.Project _project;
        private readonly VSLangProj.VSProject? _vsProject;
        private readonly string _projectTypeGuids;

        public Project(Solution solution, EnvDTE.Project project)
        {
            Solution = solution;
            _project = project;
            _vsProject = project.TryGetObject() as VSLangProj.VSProject;

            UniqueName = _project.UniqueName;

            _projectTypeGuids = _project.GetProjectTypeGuids();
        }

        public IReadOnlyCollection<ProjectReference> GetProjectReferences()
        {
            return GetProjectReferences(References);
        }

        private IReadOnlyCollection<ProjectReference> GetProjectReferences(IEnumerable<VSLangProj.Reference> references)
        {
            var projectReferences = references
                .Where(reference => reference.GetSourceProject() != null)
                .Where(reference => reference.GetCopyLocal())
                .Select(reference => new ProjectReference(Solution, reference));

            return projectReferences.ToList().AsReadOnly();
        }

        private static IReadOnlyCollection<ProjectOutput> GetLocalFileReferences(Project project, Project rootProject, bool deployExternalLocalizations, string outputDirectory, string relativeTargetDirectory)
        {
            var targetDirectory = Path.Combine(outputDirectory, relativeTargetDirectory);

            var outputs = project
                .GetBuildFiles(rootProject, BuildFileGroups.Built, outputDirectory)
                .Select(item => item.TargetName)
                .Where(File.Exists)
                .SelectMany(output => GetReferencedAssemblyNames(output, deployExternalLocalizations, targetDirectory))
                .Distinct()
                .ToList().AsReadOnly();

            var resolvedOutputs = new HashSet<string>(outputs, StringComparer.OrdinalIgnoreCase);

            foreach (var output in outputs)
            {
                ResolveReferences(Path.Combine(targetDirectory, output), deployExternalLocalizations, targetDirectory, resolvedOutputs);
            }

            var references = resolvedOutputs
                                .Select(file => new ProjectOutput(rootProject, file, relativeTargetDirectory))
                .ToList().AsReadOnly();

            return references;
        }

        private static void ResolveReferences(string filePath, bool deployExternalLocalizations, string targetDirectory, ISet<string> resolvedOutputs)
        {
            foreach (var reference in GetReferencedAssemblyNames(filePath, deployExternalLocalizations, targetDirectory))
            {
                if (resolvedOutputs.Add(reference))
                {
                    ResolveReferences(Path.Combine(targetDirectory, reference), deployExternalLocalizations, targetDirectory, resolvedOutputs);
                }
            }
        }

        public ICollection<Project> ReferencedBy { get; } = new HashSet<Project>();

        public string FullName => _project.FullName;

        [Equals(StringComparison.OrdinalIgnoreCase)]
        public string UniqueName { get; }

        public string RelativeFolder => Path.GetDirectoryName(UniqueName);

        [Lazy]
        public bool IsTestProject => _projectTypeGuids.Contains("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}") || References.Any(r => string.Equals(r.Name, "Microsoft.VisualStudio.TestPlatform.TestFramework", StringComparison.OrdinalIgnoreCase));

        public bool IsVsProject => _vsProject != null;

        public bool IsTopLevelProject => !IsTestProject && ReferencedBy.All(reference => reference.IsTestProject);

        [Lazy]
        public string? PrimaryOutputFileName => _project.ConfigurationManager?.ActiveConfiguration?.OutputGroups?.Item(BuildFileGroups.Built.ToString())?.GetFileNames().FirstOrDefault();

        public string Name => _project.Name;

        public bool IsImplicitSelected { get; private set; }

        public string? ImplicitSelectedBy { get; private set; }

        public bool UpdateIsImplicitSelected(ICollection<Project> selectedVSProjects)
        {
            if (!IsVsProject)
                return false;

            ImplicitSelectedBy = string.Join(", ", ReferencedBy
                .Where(project => selectedVSProjects.Contains(project) || project.UpdateIsImplicitSelected(selectedVSProjects))
                .Select(project => project.Name));

            return IsImplicitSelected = !string.IsNullOrEmpty(ImplicitSelectedBy);
        }

        [Lazy]
        private IReadOnlyCollection<ProjectOutput> BuildFiles => GetBuildFiles(this, AllDeployGroups, Path.GetDirectoryName(PrimaryOutputFileName) ?? string.Empty);

        [Lazy]
        private IReadOnlyCollection<VSLangProj.Reference> References => GetReferences();

        [Lazy]
        private IReadOnlyCollection<ProjectReference> ProjectReferences => GetProjectReferences();

        public IReadOnlyCollection<ProjectOutput> GetProjectOutput(bool deploySymbols, bool deployLocalizations, bool deployExternalLocalizations)
        {
            var properties = _project.ConfigurationManager?.ActiveConfiguration?.Properties;
            var outputDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_project.FullName), properties?.Item(@"OutputPath")?.Value as string ?? string.Empty));
            var relativeTargetDirectory = Path.GetDirectoryName(PrimaryOutputFileName) ?? string.Empty;

            var cache = new Dictionary<Project, IReadOnlyCollection<ProjectOutput>>();

            var projectOutput = GetProjectOutput(cache, this, deploySymbols, deployLocalizations, deployExternalLocalizations, outputDirectory, relativeTargetDirectory);

            return projectOutput;
        }

        private IReadOnlyCollection<ProjectOutput> GetProjectOutput(IDictionary<Project, IReadOnlyCollection<ProjectOutput>> cache, Project rootProject, bool deploySymbols, bool deployLocalizations, bool deployExternalLocalizations, string outputDirectory, string relativeTargetDirectory)
        {
            if (cache.TryGetValue(this, out var result))
                return result;

            var buildFileGroups = GetBuildFileGroups(deploySymbols, deployLocalizations);

            var projectOutput = BuildFiles.Where(output => (output.BuildFileGroup & buildFileGroups) != 0)
                    .Concat(GetLocalFileReferences(this, rootProject, deployExternalLocalizations, outputDirectory, relativeTargetDirectory))
                    .Concat(ProjectReferences.SelectMany(reference => reference.SourceProject?.GetProjectOutput(cache, rootProject, deploySymbols, deployLocalizations, deployExternalLocalizations, outputDirectory, relativeTargetDirectory) ?? Enumerable.Empty<ProjectOutput>()));

            result = projectOutput.ToList().AsReadOnly();

            cache[this] = result;

            return result;
        }

        private static BuildFileGroups GetBuildFileGroups(bool deploySymbols, bool deployLocalizations)
        {
            var buildFileGroups = BuildFileGroups.Built | BuildFileGroups.ContentFiles;

            if (deployLocalizations)
                buildFileGroups |= BuildFileGroups.LocalizedResourceDlls;

            if (deploySymbols)
                buildFileGroups |= BuildFileGroups.Symbols;
            return buildFileGroups;
        }

        private static IReadOnlyCollection<string> GetReferencedAssemblyNames(string assemblyFileName, bool deployExternalLocalizations, string outputDirectory)
        {
            try
            {
                var referencedAssemblyNames = AssemblyHelper.FindReferences(assemblyFileName, outputDirectory);

                var referencedAssemblyFileNames = referencedAssemblyNames
                    .Select(assemblyName => Path.GetFileName(new Uri(assemblyName.CodeBase).LocalPath))
                    .ToList().AsReadOnly();

                if (!deployExternalLocalizations)
                {
                    return referencedAssemblyFileNames;
                }

                var satteliteDlls = referencedAssemblyNames
                    .SelectMany(assemblyName => Directory.GetFiles(outputDirectory, assemblyName.Name + ".resources.dll", SearchOption.AllDirectories))
                    .Select(file => file.Substring(outputDirectory.Length))
                    .Select(file => file.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                return referencedAssemblyFileNames
                    .Concat(satteliteDlls)
                    .ToList().AsReadOnly();
            }
            catch
            {
                // assembly cannot be loaded
            }

            return Array.Empty<string>();
        }

        protected Solution Solution { get; }

        private IReadOnlyCollection<ProjectOutput> GetBuildFiles(Project rootProject, BuildFileGroups groups, string binaryTargetDirectory)
        {
            var groupNames = Enum.GetValues(typeof(BuildFileGroups)).OfType<BuildFileGroups>().Where(item => (groups & item) != 0);

            var outputGroups = _project.ConfigurationManager?.ActiveConfiguration?.OutputGroups;

            var selectedOutputGroups = groupNames
                .Select(groupName => outputGroups?.Item(groupName.ToString()))
                .ExceptNullItems();

            var buildFiles = selectedOutputGroups.SelectMany(item => GetProjectOutputForGroup(rootProject, item, binaryTargetDirectory));

            return buildFiles.ToList().AsReadOnly();
        }

        protected IReadOnlyCollection<EnvDTE.ProjectItem> GetAllProjectItems()
        {
            return _project.EnumerateAllProjectItems().ToList().AsReadOnly();
        }

        private IReadOnlyCollection<VSLangProj.Reference> GetReferences()
        {
            return GetVsProjectReferences() ?? GetMpfProjectReferences() ?? Array.Empty<VSLangProj.Reference>();
        }

        protected void AddProjectReferences(IEnumerable<Project> projects)
        {
            var referencesCollection = ReferencesCollection;

            if (referencesCollection == null)
                return;

            var existingValues = referencesCollection
                .OfType<VSLangProj.Reference>()
                .Select(r => r.GetSourceProject()?.UniqueName)
                .ExceptNullItems();

            var existingReferences = new HashSet<string>(existingValues, StringComparer.OrdinalIgnoreCase);

            var newProjects = projects
                .Where(p => !existingReferences.Contains(p.UniqueName))
                .ToList();

            foreach (var project in newProjects)
            {
                if (project == null)
                    continue;

                referencesCollection.AddProject(project._project);
            }
        }

        protected void RemoveProjectReferences(IEnumerable<Project> projects)
        {
            var references = References.Where(item => item.SourceProject != null)
                .ToDictionary(item => item.SourceProject.UniqueName, StringComparer.OrdinalIgnoreCase);

            var projectReferences = projects
                // ReSharper disable once SuspiciousTypeConversion.Global
                .Select(project => references.GetValueOrDefault(project.UniqueName))
                .ToList().AsReadOnly();

            foreach (var reference in projectReferences)
            {
                reference?.Remove();
            }
        }

        protected EnvDTE.ProjectItem AddItemFromFile(string fileName)
        {
            return _project.ProjectItems.AddFromFile(fileName);
        }

        private VSLangProj.References? ReferencesCollection
        {
            get
            {
                try
                {
                    if (_vsProject != null)
                        return _vsProject.References;

                    var projectItems = _project.ProjectItems;

                    return projectItems?
                        .OfType<EnvDTE.ProjectItem>()
                        .Select(p => p.Object)
                        .OfType<VSLangProj.References>()
                        .FirstOrDefault();
                }
                catch (ExternalException)
                {
                }

                return null;
            }
        }

        private IReadOnlyCollection<VSLangProj.Reference>? GetMpfProjectReferences()
        {
            try
            {
                var projectItems = _project.ProjectItems;

                return projectItems?
                    .OfType<EnvDTE.ProjectItem>()
                    .Select(p => p.Object)
                    .OfType<VSLangProj.References>()
                    .Take(1)
                                        .SelectMany(references => references.OfType<VSLangProj.Reference>())
                    .ToList().AsReadOnly();
            }
            catch
            {
                return null;
            }
        }

        private IReadOnlyCollection<VSLangProj.Reference>? GetVsProjectReferences()
        {
            try
            {
                return _vsProject?
                    .References?
                    .OfType<VSLangProj.Reference>()
                    .ToList().AsReadOnly();
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyCollection<ProjectOutput> GetProjectOutputForGroup(Project project, EnvDTE.OutputGroup outputGroup, string binaryTargetDirectory)
        {
            var canonicalName = outputGroup.CanonicalName;

            if (!Enum.TryParse(canonicalName, out BuildFileGroups buildFileGroup))
                throw new InvalidOperationException("Unknown output group: " + canonicalName);

            var fileNames = outputGroup.GetFileNames();

            var projectOutputForGroup = fileNames.Select(fileName => new ProjectOutput(project, fileName, buildFileGroup, binaryTargetDirectory));

            return projectOutputForGroup.ToList().AsReadOnly();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator, UsedImplicitly]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return UniqueName;
        }
    }

    internal static class ProjectExtension
    {
        public static EnvDTE.Project? GetSourceProject(this VSLangProj.Reference reference)
        {
            try
            {
                return reference.SourceProject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IEnumerable<string> GetFileNames(this EnvDTE.OutputGroup outputGroup)
        {
            return InternalGetFileNames(outputGroup) ?? Array.Empty<string>();
        }

        private static IReadOnlyCollection<string>? InternalGetFileNames(this EnvDTE.OutputGroup outputGroup)
        {
            try
            {
                return ((Array)outputGroup.FileNames)?.OfType<string>().ToList().AsReadOnly();
            }
            catch
            {
                return null;
            }
        }
    }
}
