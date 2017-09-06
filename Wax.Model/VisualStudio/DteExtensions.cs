namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell.Flavor;
    using Microsoft.VisualStudio.Shell.Interop;

    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    internal static class DteExtensions
    {
        /// <summary>
        /// Gets all projects in the solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <returns>The projects.</returns>
        [NotNull, ItemNotNull]
        public static IReadOnlyCollection<EnvDTE.Project> GetProjects([NotNull] this EnvDTE.Solution solution)
        {
            Contract.Requires(solution != null);
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.Project>>() != null);

            var items = new List<EnvDTE.Project>();

            var projects = solution.Projects;

            if (projects == null)
                return items;

            for (var i = 1; i <= projects.Count; i++)
            {
                try
                {
                    var project = projects.Item(i);
                    if (project == null)
                        continue;

                    items.Add(project);

                    project.ProjectItems.GetSubProjects(items);
                }
                catch
                {
                    Trace.TraceError("Error loading project #" + i);
                }
            }

            return items;
        }

        private static void GetSubProjects([CanBeNull] this IEnumerable projectItems, [NotNull] ICollection<EnvDTE.Project> items)
        {
            Contract.Requires(items != null);

            if (projectItems == null)
                return;

            foreach (var projectItem in projectItems.OfType<EnvDTE.ProjectItem>())
            {
                projectItem.GetSubProjects(items);
            }
        }

        private static void GetSubProjects([CanBeNull] this EnvDTE.ProjectItem projectItem, [NotNull] ICollection<EnvDTE.Project> items)
        {
            Contract.Requires(items != null);

            var subProject = projectItem?.SubProject;

            if (subProject == null)
                return;

            items.Add(subProject);

            subProject.ProjectItems.GetSubProjects(items);
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<EnvDTE.ProjectItem> EnumerateAllProjectItems([NotNull] this EnvDTE.Project project)
        {
            Contract.Requires(project != null);
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (project.ProjectItems == null)
                yield break;

            foreach (var item in project.ProjectItems.OfType<EnvDTE.ProjectItem>())
            {
                yield return item;

                foreach (var subItem in EnumerateProjectItems(item))
                {
                    yield return subItem;
                }
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<EnvDTE.ProjectItem> EnumerateProjectItems([NotNull] EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (projectItem.ProjectItems == null)
                yield break;

            foreach (var item in projectItem.ProjectItems.OfType<EnvDTE.ProjectItem>())
            {
                yield return item;

                foreach (var subItem in EnumerateProjectItems(item))
                {
                    yield return subItem;
                }
            }
        }

        [NotNull]
        public static string GetProjectTypeGuids([NotNull] this EnvDTE.Project proj)
        {
            Contract.Requires(proj != null);
            Contract.Ensures(Contract.Result<string>() != null);

            try
            {
                var dte = proj.TryGetDte();
                if (dte == null)
                    return string.Empty;

                var solution = GetService<IVsSolution>(dte);

                if (solution == null)
                    return string.Empty;

                var result = solution.GetProjectOfUniqueName(proj.UniqueName, out IVsHierarchy hierarchy);

                if (result == 0)
                {
                    if (hierarchy is IVsAggregatableProjectCorrected aggregatableProject)
                    {
                        result = aggregatableProject.GetAggregateProjectTypeGuids(out string projectTypeGuids);

                        if ((result == 0) && (projectTypeGuids != null))
                            return projectTypeGuids;

                    }
                }
            }
            catch
            {
                // internal error
            }

            return string.Empty;
        }

        [CanBeNull]
        private static T GetService<T>([NotNull] object serviceProvider) where T : class
        {
            Contract.Requires(serviceProvider != null);

            return (T)GetService((IServiceProvider)serviceProvider, typeof(T).GUID);
        }

        [CanBeNull]
        private static object GetService([NotNull] IServiceProvider serviceProvider, Guid guid)
        {
            Contract.Requires(serviceProvider != null);

            IntPtr serviceHandle;
            var hr = serviceProvider.QueryService(guid, guid, out serviceHandle);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceHandle.Equals(IntPtr.Zero))
            {
                var service = Marshal.GetObjectForIUnknown(serviceHandle);
                Marshal.Release(serviceHandle);
                return service;
            }

            return null;
        }

        [CanBeNull]
        public static string TryGetFileName([NotNull] this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            try
            {
                // some items report a file count > 0 but don't return a file name!
                if (projectItem.FileCount > 0)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    return projectItem.FileNames[0];
                }
            }
            catch (Exception)
            {
                // s.a.
            }

            return null;
        }

        [NotNull]
        public static XDocument GetXmlContent([NotNull] this EnvDTE.ProjectItem projectItem, LoadOptions loadOptions)
        {
            Contract.Requires(projectItem != null);
            Contract.Ensures(Contract.Result<XDocument>() != null);

            return XDocument.Parse(projectItem.GetContent(), loadOptions);
        }

        [NotNull]
        [ContractVerification(false)]
        public static string GetContent([NotNull] this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);
            Contract.Ensures(Contract.Result<string>() != null);

            try
            {
                if (!projectItem.IsOpen)
                    projectItem.Open();

                var document = projectItem.Document;

                if (document != null)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return GetContent((EnvDTE.TextDocument)document.Object("TextDocument"));
                }

                var fileName = projectItem.TryGetFileName();

                if (string.IsNullOrEmpty(fileName))
                    return string.Empty;

                return File.ReadAllText(fileName);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        [NotNull]
        [ContractVerification(false), SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static string GetContent([NotNull] EnvDTE.TextDocument document)
        {
            Contract.Requires(document != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
        }

        public static void SetContent([NotNull] this EnvDTE.ProjectItem projectItem, [NotNull] string text)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(text != null);

            if (!projectItem.IsOpen)
                projectItem.Open(EnvDTE.Constants.vsViewKindCode);

            var document = projectItem.Document;

            if (document != null)
            {
                SetContent(document, text);
            }
            else
            {
                var fileName = projectItem.TryGetFileName();

                if (string.IsNullOrEmpty(fileName))
                    return;

                File.WriteAllText(fileName, text);
            }
        }

        [ContractVerification(false), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static void SetContent([NotNull] EnvDTE.Document document, [CanBeNull] string text)
        {
            Contract.Requires(document != null);

            var textDocument = (EnvDTE.TextDocument)document.Object("TextDocument");

            textDocument.StartPoint.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);
        }

        [CanBeNull]
        public static object TryGetObject([CanBeNull] this EnvDTE.Project project)
        {
            try
            {
                return project?.Object;
            }
            catch
            {
                return null;
            }
        }

        [CanBeNull]
        public static EnvDTE.DTE TryGetDte([CanBeNull] this EnvDTE.Project project)
        {
            try
            {
                return project?.DTE;
            }
            catch
            {
                return null;
            }
        }
    }
}
