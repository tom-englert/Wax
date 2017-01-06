namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    public class ProjectOutput : IEquatable<ProjectOutput>
    {
        [NotNull]
        private readonly string _relativeFileName;
        [NotNull]
        private readonly Project _project;
        private readonly bool _isReference;
        public bool RedirectToNonStandardOutput;

        public ProjectOutput([NotNull] Project project, [NotNull] string relativeFileName, BuildFileGroups buildFileGroup, [NotNull] string binaryTargetDirectory)
        {
            Contract.Requires(project != null);
            Contract.Requires(relativeFileName != null);

            if (buildFileGroup == BuildFileGroups.Built || buildFileGroup == BuildFileGroups.Symbols)
            {
                RedirectToNonStandardOutput = true;
                // Build output should be only a file name, without folder.
                // => Workaround: In Web API projects (ASP.NET MVC) the build output is always "bin\<targetname>.dll" instead of just "<targetname>.dll",
                // where "bin" seems to be hard coded.
                // In this case, Wax needs to only get the file name as source and to remember the specil output dir as target for every dll and pdb files
                if (removeNonStandardOutput && fullName != Path.GetFileName(fullName))
                {
                    project.HasNonStandardOutput = true;
                    project.NonStandardOutputPath = Path.GetDirectoryName(fullName);
                    fullName = (Path.GetFileName(fullName));
                }
            }

            _fullName = fullName;
            _project = project;

            var prefix = binaryTargetDirectory + @"\";

            if ((buildFileGroup != BuildFileGroups.ContentFiles) && relativeFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _relativeFileName = relativeFileName.Substring(prefix.Length);
            }
            else
            {
                _relativeFileName = relativeFileName;
            }

            BuildFileGroup = buildFileGroup;
            _binaryTargetDirectory = binaryTargetDirectory ?? string.Empty;
        }

        public ProjectOutput([NotNull] Project project, [NotNull] string fullName)
        {
            Contract.Requires(project != null);
            Contract.Requires(fullName != null);
            RedirectToNonStandardOutput = true;
            _isReference = true;
            _fullName = fullName;
            _project = project;
            _relativeFileName = Path.GetFileName(fullPath);
            _binaryTargetDirectory = binaryTargetDirectory;
        }

        public ProjectOutput([NotNull] Project project, [NotNull] VSLangProj.Reference reference)
        {
            Contract.Requires(project != null);
            Contract.Requires(reference != null);
            RedirectToNonStandardOutput = true;
            _isReference = true;
            Contract.Assume(reference.Path != null);
            _fullName = reference.Path;
            _project = project;
        }

        [NotNull]
        public string FullName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                if (BuildFileGroup == BuildFileGroups.ContentFiles)
                    return _relativeFileName;

                return Path.Combine(_binaryTargetDirectory, _relativeFileName);
            }
        }

        [NotNull]
        public string FileName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return Path.GetFileName(_relativeFileName);
            }
        }

        [NotNull]
        public Project Project
        {
            get
            {
                Contract.Ensures(Contract.Result<Project>() != null);

                return _project;
            }
        }

        public bool IsReference => _isReference;

        public override string ToString()
        {
            return _relativeFileName;
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
            return _relativeFileName.ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectOutput);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectOutput"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectOutput"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectOutput"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectOutput other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ProjectOutput left, ProjectOutput right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return string.Equals(left._relativeFileName, right._relativeFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ProjectOutput left, ProjectOutput right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectOutput left, ProjectOutput right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_relativeFileName != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_binaryTargetDirectory != null);
        }

    }
}
