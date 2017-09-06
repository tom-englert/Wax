namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using JetBrains.Annotations;

    using Mono.Cecil;

    public class Project : IEquatable<Project>
    {
        [NotNull]
        private readonly Solution _solution;
        [NotNull]
        private readonly EnvDTE.Project _project;
        private readonly VSLangProj.VSProject _vsProject;
        [NotNull, ItemNotNull]
        private readonly ICollection<Project> _referencedBy = new HashSet<Project>();
        [NotNull]
        private readonly string _uniqueName;
        [NotNull]
        private readonly string _projectTypeGuids;

        public Project([NotNull] Solution solution, [NotNull] EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _solution = solution;
            _project = project;
            _vsProject = project.TryGetObject() as VSLangProj.VSProject;

            Contract.Assume(_project.UniqueName != null);
            _uniqueName = _project.UniqueName;

            _projectTypeGuids = _project.GetProjectTypeGuids();
        }

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ProjectReference> GetProjectReferences()
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectReference>>() != null);

            return GetProjectReferences(GetReferences());
        }

        [NotNull, ItemNotNull]
        private IReadOnlyCollection<ProjectReference> GetProjectReferences([NotNull, ItemNotNull] IEnumerable<VSLangProj.Reference> references)
        {
            Contract.Requires(references != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectReference>>() != null);

            var projectReferences = references
                .Where(reference => reference.GetSourceProject() != null)
                .Where(reference => reference.CopyLocal)
                .Select(reference => new ProjectReference(Solution, reference));

            return projectReferences.ToArray();
        }

        [NotNull, ItemNotNull]
        private static IReadOnlyCollection<ProjectOutput> GetLocalFileReferences([NotNull] Project rootProject, bool deployExternalLocalizations, [NotNull, ItemNotNull] IReadOnlyCollection<VSLangProj.Reference> references, [NotNull] string targetDirectory)
        {
            Contract.Requires(rootProject != null);
            Contract.Requires(references != null);
            Contract.Requires(targetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var localFileReferences = references
                .Where(reference => reference.GetSourceProject() == null)
                .Where(reference => reference.CopyLocal)
                .Where(reference => !string.IsNullOrEmpty(reference.Path))
                .Select(reference => new ProjectOutput(rootProject, reference, targetDirectory))
                .Concat(GetSecondTierReferences(references, rootProject, deployExternalLocalizations, targetDirectory));

            return localFileReferences.ToArray();
        }

        [NotNull, ItemNotNull]
        public ICollection<Project> ReferencedBy
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<Project>>() != null);

                return _referencedBy;
            }
        }

        [NotNull]
        public string FullName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var fullName = _project.FullName;
                Contract.Assume(fullName != null);
                return fullName;
            }
        }

        [NotNull]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _uniqueName;
            }
        }

        [NotNull]
        public string RelativeFolder
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetDirectoryName(_uniqueName);
            }
        }

        public bool IsTestProject => _projectTypeGuids.Contains("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}");

        public bool IsVsProject => _vsProject != null;

        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ProjectOutput> GetProjectOutput(bool deploySymbols, bool deployExternalLocalizations)
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var primaryOutput = _project.ConfigurationManager?.ActiveConfiguration?.OutputGroups?.Item(BuildFileGroups.Built.ToString())?.GetFileNames().FirstOrDefault();

            var binaryTargetDirectory = Path.GetDirectoryName(primaryOutput) ?? string.Empty;

            return GetProjectOutput(this, deploySymbols, deployExternalLocalizations, binaryTargetDirectory);
        }

        [NotNull, ItemNotNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private IReadOnlyCollection<ProjectOutput> GetProjectOutput([NotNull] Project rootProject, bool deploySymbols, bool deployExternalLocalizations, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(rootProject != null);
            Contract.Requires(binaryTargetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var references = GetReferences();

            var projectOutput = GetBuildFiles(rootProject, deploySymbols, binaryTargetDirectory)
                .Concat(GetLocalFileReferences(rootProject, deployExternalLocalizations, references, binaryTargetDirectory)) // references must go to the same folder as the referencing component.
                .Concat(GetProjectReferences(references).SelectMany(reference => reference.SourceProject?.GetProjectOutput(rootProject, deploySymbols, deployExternalLocalizations, binaryTargetDirectory) ?? Enumerable.Empty<ProjectOutput>()));

            return projectOutput.ToArray();
        }

        [NotNull, ItemNotNull]
        private static IReadOnlyCollection<ProjectOutput> GetSecondTierReferences([NotNull, ItemNotNull] IEnumerable<VSLangProj.Reference> references, [NotNull] Project rootProject, bool deployExternalLocalizations, [NotNull] string targetDirectory)
        {
            Contract.Requires(references != null);
            Contract.Requires(rootProject != null);
            Contract.Requires(targetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            // Try to resolve second-tier references for CopyLocal references
            return references
                .Where(reference => reference.CopyLocal)
                .Select(reference => reference.Path)
                .Where(File.Exists) // Reference can be a project reference, but project has not been built yet.
                .SelectMany(file => GetReferencedAssemblyNames(file, deployExternalLocalizations))
                .Distinct()
                .Select(file => new ProjectOutput(rootProject, file, targetDirectory))
                .ToArray();
        }

        [NotNull, ItemNotNull]
        [ContractVerification(false), SuppressMessage("ReSharper", "PossibleNullReferenceException"), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static IReadOnlyCollection<string> GetReferencedAssemblyNames([NotNull] string assemblyFileName, bool deployExternalLocalizations)
        {
            Contract.Requires(assemblyFileName != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            try
            {
                var directory = Path.GetDirectoryName(assemblyFileName);

                var referencedAssemblyNames = AssemblyDefinition.ReadAssembly(assemblyFileName)
                    .MainModule
                    .AssemblyReferences
                    .Select(reference => reference.Name)
                    .Where(assemblyName => File.Exists(Path.Combine(directory, assemblyName + ".dll")))
                    .ToArray();

                var referencedAssemblyFileNames = referencedAssemblyNames
                    .Select(assemblyName => assemblyName + ".dll")
                    .ToArray();

                if (!deployExternalLocalizations)
                {
                    return referencedAssemblyFileNames;
                }

                var satteliteDlls = referencedAssemblyNames
                    .SelectMany(assemblyName => Directory.GetFiles(directory, assemblyName + ".resources.dll", SearchOption.AllDirectories))
                    .Select(file => file.Substring(directory.Length + 1));

                return referencedAssemblyFileNames
                    .Concat(satteliteDlls)
                    .ToArray();
            }
            catch
            {
                // assembly cannot be loaded
            }

            return new string[0];
        }

        [NotNull]
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var name = _project.Name;
                Contract.Assume(name != null);
                return name;
            }
        }

        [NotNull]
        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        [NotNull, ItemNotNull]
        private IReadOnlyCollection<ProjectOutput> GetBuildFiles([NotNull] Project rootProject, bool deploySymbols, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(rootProject != null);
            Contract.Requires(binaryTargetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var buildFileGroups = BuildFileGroups.Built | BuildFileGroups.ContentFiles | BuildFileGroups.LocalizedResourceDlls;

            if (deploySymbols)
                buildFileGroups |= BuildFileGroups.Symbols;

            return GetBuildFiles(rootProject, buildFileGroups, binaryTargetDirectory);
        }

        [NotNull, ItemNotNull]
        private IReadOnlyCollection<ProjectOutput> GetBuildFiles([NotNull] Project rootProject, BuildFileGroups groups, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(rootProject != null);
            Contract.Requires(binaryTargetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var groupNames = Enum.GetValues(typeof(BuildFileGroups)).OfType<BuildFileGroups>().Where(item => (groups & item) != 0);

            var outputGroups = _project.ConfigurationManager?.ActiveConfiguration?.OutputGroups;

            var selectedOutputGroups = groupNames
                .Select(groupName => outputGroups?.Item(groupName.ToString()))
                .Where(item => item != null);

            var buildFiles = selectedOutputGroups.SelectMany(item => GetProjectOutputForGroup(rootProject, item, binaryTargetDirectory));

            return buildFiles.ToArray();
        }

        [NotNull, ItemNotNull]
        protected IReadOnlyCollection<EnvDTE.ProjectItem> GetAllProjectItems()
        {
            Contract.Ensures(Contract.Result<IReadOnlyCollection<EnvDTE.ProjectItem>>() != null);

            return _project.EnumerateAllProjectItems().ToArray();
        }

        [NotNull, ItemNotNull]
        private IReadOnlyCollection<VSLangProj.Reference> GetReferences()
        {
            Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

            return GetVsProjectReferences() ?? GetMpfProjectReferences() ?? new VSLangProj.Reference[0];
        }

        protected void AddProjectReferences([NotNull] params Project[] projects)
        {
            Contract.Requires(projects != null);

            var referencesCollection = ReferencesCollection;

            if (referencesCollection == null)
                return;

            var projectReferences = GetReferences()
                .Where(r => r.GetSourceProject() != null)
                .ToArray();

            var newProjects = projects
                .Where(project => projectReferences.All(reference => !Equals(reference.GetSourceProject(), project)))
                .ToArray();

            foreach (var project in newProjects)
            {
                if (project == null)
                    continue;

                referencesCollection.AddProject(project._project);
            }
        }

        protected internal void RemoveProjectReferences([NotNull] params Project[] projects)
        {
            Contract.Requires(projects != null);

            var references = GetReferences();

            var projectReferences = projects
                .Select(project => references.FirstOrDefault(reference => Equals(reference.GetSourceProject(), project)))
                .ToArray();

            foreach (var reference in projectReferences)
            {
                reference?.Remove();
            }
        }

        [NotNull]
        [ContractVerification(false), SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "No contracts for EnvDTE"), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        protected EnvDTE.ProjectItem AddItemFromFile([NotNull] string fileName)
        {
            Contract.Requires(fileName != null);
            Contract.Ensures(Contract.Result<EnvDTE.ProjectItem>() != null);

            return _project.ProjectItems.AddFromFile(fileName);
        }

        [ContractVerification(false)]
        private VSLangProj.References ReferencesCollection
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

        [ContractVerification(false)]
        private IReadOnlyCollection<VSLangProj.Reference> GetMpfProjectReferences()
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
                    .ToArray();
            }
            catch
            {
                return null;
            }
        }

        [ContractVerification(false)]
        private IReadOnlyCollection<VSLangProj.Reference> GetVsProjectReferences()
        {
            try
            {
                return _vsProject?
                    .References?
                    .OfType<VSLangProj.Reference>()
                    .ToArray();
            }
            catch
            {
                return null;
            }
        }

        [NotNull, ItemNotNull]
        private static IReadOnlyCollection<ProjectOutput> GetProjectOutputForGroup([NotNull] Project project, [NotNull] EnvDTE.OutputGroup outputGroup, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(outputGroup != null);
            Contract.Requires(binaryTargetDirectory != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            BuildFileGroups buildFileGroup;
            var canonicalName = outputGroup.CanonicalName;

            if (!Enum.TryParse(canonicalName, out buildFileGroup))
                throw new InvalidOperationException("Unknown output group: " + canonicalName);

            var fileNames = outputGroup.GetFileNames();

            var projectOutputForGroup = fileNames.Select(fileName => new ProjectOutput(project, fileName, buildFileGroup, binaryTargetDirectory));

            return projectOutputForGroup.ToArray();
        }

        public override string ToString()
        {
            return UniqueName;
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return UniqueName.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Project);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Project"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Project"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="Project"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(Project other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(Project left, Project right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return string.Equals(left.UniqueName, right.UniqueName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(Project left, Project right)
        {
            return InternalEquals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(Project left, Project right)
        {
            return !InternalEquals(left, right);
        }

        //private static bool Equals(Project p1, EnvDTE.Project p2)
        //{
        //    return Equals(p2, p1);
        //}

        private static bool Equals(EnvDTE.Project left, Project right)
        {
            if (ReferenceEquals(left, null))
                return (ReferenceEquals(right, null));
            if (ReferenceEquals(right, null))
                return false;

            return string.Equals(left.UniqueName, right.UniqueName, StringComparison.OrdinalIgnoreCase);
        }


        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_projectTypeGuids != null);
            Contract.Invariant(_referencedBy != null);
            Contract.Invariant(_uniqueName != null);
        }
    }

    internal static class ProjectExtension
    {
        public static EnvDTE.Project GetSourceProject([NotNull] this VSLangProj.Reference reference)
        {
            Contract.Requires(reference != null);

            try
            {
                return reference.SourceProject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [NotNull, ItemNotNull]
        public static string[] GetFileNames([NotNull] this EnvDTE.OutputGroup outputGroup)
        {
            Contract.Requires(outputGroup != null);
            Contract.Ensures(Contract.Result<string[]>() != null);

            return InternalGetFileNames(outputGroup) ?? new string[0];
        }

        private static string[] InternalGetFileNames([NotNull] this EnvDTE.OutputGroup outputGroup)
        {
            Contract.Requires(outputGroup != null);

            try
            {
                return ((Array)outputGroup.FileNames)?.OfType<string>().ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
