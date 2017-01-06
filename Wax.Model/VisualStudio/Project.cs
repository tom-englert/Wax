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

        public bool HasNonStandardOutput;

        public string NonStandardOutputPath;

        public Project([NotNull] Solution solution, [NotNull] EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _solution = solution;
            _project = project;
            _vsProject = project.Object as VSLangProj.VSProject;

            Contract.Assume(_project.UniqueName != null);
            _uniqueName = _project.UniqueName;

            _projectTypeGuids = _project.GetProjectTypeGuids();
        }

        [NotNull, ItemNotNull]
        public IEnumerable<ProjectReference> ProjectReferences
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ProjectReference>>() != null);

                var projectReferences = References
                    .Where(reference => reference.GetSourceProject() != null)
                    .Where(reference => reference.CopyLocal)
                    .Select(reference => new ProjectReference(Solution, reference));

                return projectReferences;
            }
        }

        [NotNull, ItemNotNull]
        public IEnumerable<ProjectOutput> GetLocalFileReferences([NotNull] Project rootProject)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var localFileReferences = References
                .Where(reference => reference.GetSourceProject() == null)
                .Where(reference => reference.CopyLocal)
                .Select(reference => new ProjectOutput(rootProject, reference))
                .Concat(GetSecondTierReferences(rootProject));

            return localFileReferences;
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
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public IEnumerable<ProjectOutput> GetProjectOutput([NotNull] Project rootProject, bool deploySymbols, bool removeNonStandardOutput)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var projectOutput = GetBuildFiles(rootProject, deploySymbols, removeNonStandardOutput)
                .Concat(GetLocalFileReferences(rootProject))
                .Concat(ProjectReferences.SelectMany(reference => reference.SourceProject.GetProjectOutput(rootProject, deploySymbols, removeNonStandardOutput)));

            return projectOutput;
        }

        [NotNull, ItemNotNull]
        private IEnumerable<ProjectOutput> GetSecondTierReferences([NotNull] Project rootProject)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            // Try to resolve second-tier references for CopyLocal references
            return References
                .Where(r => r.CopyLocal)
                .Select(r => r.Path)
                .Where(File.Exists) // Reference can be a project reference but not be built yet.
                .SelectMany(GetReferencedAssemblyNames)
                .Distinct()
                .Where(File.Exists)
                .Select(file => new ProjectOutput(rootProject, file));
        }

        [NotNull]
        [ContractVerification(false), SuppressMessage("ReSharper", "PossibleNullReferenceException"), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static IEnumerable<string> GetReferencedAssemblyNames([NotNull] string assemblyFileName)
        {
            Contract.Requires(assemblyFileName != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            try
            {
                var directory = Path.GetDirectoryName(assemblyFileName);

                return AssemblyDefinition.ReadAssembly(assemblyFileName)
                    .MainModule
                    .AssemblyReferences
                    .Select(reference => Path.Combine(directory, reference.Name + ".dll"));
            }
            catch
            {
                // assembly cannot be loaded
            }

            return Enumerable.Empty<string>();
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
        public IEnumerable<ProjectOutput> GetBuildFiles([NotNull] Project rootProject, bool deploySymbols, bool removeNonStandardOutput)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var buildFileGroups = BuildFileGroups.Built | BuildFileGroups.ContentFiles | BuildFileGroups.LocalizedResourceDlls;

            if (deploySymbols)
                buildFileGroups |= BuildFileGroups.Symbols;

            return GetBuildFiles(rootProject, buildFileGroups, removeNonStandardOutput);
        }

        [NotNull, ItemNotNull]
        public IEnumerable<ProjectOutput> GetBuildFiles([NotNull] Project rootProject, BuildFileGroups groups, bool removeNonStandardOutput)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var groupNames = Enum.GetValues(typeof(BuildFileGroups)).OfType<BuildFileGroups>().Where(item => (groups & item) != 0);

            var configurationManager = _project.ConfigurationManager;
            Contract.Assume(configurationManager != null);
            var activeConfiguration = configurationManager.ActiveConfiguration;
            Contract.Assume(activeConfiguration != null);
            var outputGroups = activeConfiguration.OutputGroups;
            var selectedOutputGroups = groupNames.Select(groupName => outputGroups.Item(groupName.ToString()));

            var buildFiles = selectedOutputGroups.SelectMany(item => GetProjectOutputForGroup(rootProject, item, removeNonStandardOutput));

            return buildFiles;
        }

        [NotNull, ItemNotNull]
        protected internal IEnumerable<EnvDTE.ProjectItem> AllProjectItems
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

                return _project.GetAllProjectItems();
            }
        }

        [NotNull, ItemNotNull]
        protected internal IEnumerable<VSLangProj.Reference> References
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

                return VsProjectReferences ?? MpfProjectReferences ?? Enumerable.Empty<VSLangProj.Reference>();
            }
        }

        protected internal void AddProjectReferences([NotNull] params Project[] projects)
        {
            Contract.Requires(projects != null);

            var referencesCollection = ReferencesCollection;

            if (referencesCollection == null)
                return;

            var projectReferences = References
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

            var references = References;

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
                    Contract.Assume(projectItems != null);

                    return projectItems
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
        private IEnumerable<VSLangProj.Reference> MpfProjectReferences
        {
            get
            {
                try
                {
                    var projectItems = _project.ProjectItems;
                    Contract.Assume(projectItems != null);

                    return projectItems
                        .OfType<EnvDTE.ProjectItem>()
                        .Select(p => p.Object)
                        .OfType<VSLangProj.References>()
                        .Take(1)
                        .SelectMany(references => references.Cast<VSLangProj.Reference>());
                }
                catch
                {
                    return null;
                }
            }
        }

        [ContractVerification(false)]
        private IEnumerable<VSLangProj.Reference> VsProjectReferences
        {
            get
            {
                try
                {
                    return _vsProject?.References?.Cast<VSLangProj.Reference>();
                }
                catch
                {
                    return null;
                }
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<ProjectOutput> GetProjectOutputForGroup([NotNull] Project project, [NotNull] EnvDTE.OutputGroup outputGroup, bool removeNonStandardOutput)
        {
            Contract.Requires(project != null);
            Contract.Requires(outputGroup != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            BuildFileGroups buildFileGroup;
            var canonicalName = outputGroup.CanonicalName;

            if (!Enum.TryParse(canonicalName, out buildFileGroup))
                throw new InvalidOperationException("Unknown output group: " + canonicalName);

            var fileNames = outputGroup.GetFileNames();

            var projectOutputForGroup = fileNames.Select(fileName => new ProjectOutput(project, fileName, buildFileGroup, removeNonStandardOutput));

            return projectOutputForGroup;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
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

            Contract.Assume(outputGroup.FileNames != null);

            return ((Array)outputGroup.FileNames).OfType<string>().ToArray();
        }
    }
}
