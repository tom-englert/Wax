namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    public class Project : IEquatable<Project>
    {
        private readonly Solution _solution;
        private readonly EnvDTE.Project _project;
        private readonly VSLangProj.VSProject _vsProject;
        private readonly ICollection<Project> _referencedBy = new HashSet<Project>();
        private readonly string _uniqueName;
        private readonly string _projectTypeGuids;

        public Project(Solution solution, EnvDTE.Project project)
        {
            Contract.Requires(solution != null);
            Contract.Requires(project != null);

            _solution = solution;
            _project = project;
            _vsProject = project.Object as VSLangProj.VSProject;

            _uniqueName = _project.UniqueName;
            Contract.Assume(_uniqueName != null);

            _projectTypeGuids = _project.GetProjectTypeGuids();
        }

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

        public IEnumerable<ProjectOutput> GetLocalFileReferences(Project rootProject)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var localFileReferences = References
                .Where(reference => reference.GetSourceProject() == null)
                .Where(reference => reference.CopyLocal)
                .Select(reference => new ProjectOutput(rootProject, reference));

            return localFileReferences;
        }

        public ICollection<Project> ReferencedBy
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<Project>>() != null);

                return _referencedBy;
            }
        }

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

        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _uniqueName;
            }
        }

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

        public IEnumerable<ProjectOutput> GetProjectOutput(Project rootProject, bool deploySymbols)
        {
            Contract.Requires(rootProject != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var projectOutput = GetBuildFiles(rootProject, deploySymbols)
                .Concat(GetLocalFileReferences(rootProject))
                .Concat(ProjectReferences.SelectMany(reference => reference.SourceProject.GetProjectOutput(rootProject, deploySymbols)))
                .Distinct()
                .OrderBy(output => output.FullName, StringComparer.OrdinalIgnoreCase);

            return projectOutput;
        }

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

        public Solution Solution
        {
            get
            {
                Contract.Ensures(Contract.Result<Solution>() != null);

                return _solution;
            }
        }

        public IEnumerable<ProjectOutput> GetBuildFiles(Project rootProject, bool deploySymbols)
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);


            var buildFileGroups = BuildFileGroups.Built | BuildFileGroups.ContentFiles | BuildFileGroups.LocalizedResourceDlls;

            if (deploySymbols)
                buildFileGroups |= BuildFileGroups.Symbols;

            return GetBuildFiles(rootProject, buildFileGroups);
        }

        public IEnumerable<ProjectOutput> GetBuildFiles(Project rootProject, BuildFileGroups groups)
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectOutput>>() != null);

            var groupNames = Enum.GetValues(typeof(BuildFileGroups)).Cast<BuildFileGroups>().Where(item => (groups & item) != 0);

            var configurationManager = _project.ConfigurationManager;
            Contract.Assume(configurationManager != null);
            var activeConfiguration = configurationManager.ActiveConfiguration;
            Contract.Assume(activeConfiguration != null);
            var outputGroups = activeConfiguration.OutputGroups;
            var selectedOutputGroups = groupNames.Select(groupName => outputGroups.Item(groupName.ToString()));

            var buildFiles = selectedOutputGroups.SelectMany(item => GetProjectOutputForGroup(rootProject, item));

            return buildFiles;
        }

        protected internal IEnumerable<EnvDTE.ProjectItem> AllProjectItems
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

                return _project.GetAllProjectItems();
            }
        }

        protected internal IEnumerable<VSLangProj.Reference> References
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<VSLangProj.Reference>>() != null);

                return VsProjectReferences ?? MpfProjectReferences ?? Enumerable.Empty<VSLangProj.Reference>();
            }
        }

        protected internal void AddProjectReferences(params Project[] projects)
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

        protected internal void RemoveProjectReferences(params Project[] projects)
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
                        .Cast<EnvDTE.ProjectItem>()
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
                        .Cast<EnvDTE.ProjectItem>()
                        .Select(p => p.Object)
                        .OfType<VSLangProj.References>()
                        .Take(1)
                        .SelectMany(references => references.Cast<VSLangProj.Reference>());
                }
                catch
                {
                }

                return null;
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

        private static IEnumerable<ProjectOutput> GetProjectOutputForGroup(Project project, EnvDTE.OutputGroup outputGroup)
        {
            Contract.Requires(outputGroup != null);

            BuildFileGroups buildFileGroup;
            var canonicalName = outputGroup.CanonicalName;

            if (!Enum.TryParse(canonicalName, out buildFileGroup))
                throw new InvalidOperationException("Unknown output group: " + canonicalName);

            var fileNames = (Array)outputGroup.FileNames;

            Contract.Assume(fileNames != null);

            var projectOutputForGroup = fileNames.Cast<string>().Select(fileName => new ProjectOutput(project, fileName, buildFileGroup));

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
        public static EnvDTE.Project GetSourceProject(this VSLangProj.Reference reference)
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
    }
}
