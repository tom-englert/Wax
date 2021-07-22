namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

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
        public static IReadOnlyCollection<EnvDTE.Project> GetProjects(this EnvDTE.Solution solution)
        {
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

        private static void GetSubProjects(this IEnumerable? projectItems, ICollection<EnvDTE.Project> items)
        {
            if (projectItems == null)
                return;

            foreach (var projectItem in projectItems.OfType<EnvDTE.ProjectItem>())
            {
                projectItem.GetSubProjects(items);
            }
        }

        private static void GetSubProjects(this EnvDTE.ProjectItem? projectItem, ICollection<EnvDTE.Project> items)
        {
            var subProject = projectItem?.SubProject;

            if (subProject == null)
                return;

            items.Add(subProject);

            subProject.ProjectItems.GetSubProjects(items);
        }

        public static IEnumerable<EnvDTE.ProjectItem> EnumerateAllProjectItems(this EnvDTE.Project project)
        {
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

        private static IEnumerable<EnvDTE.ProjectItem> EnumerateProjectItems(EnvDTE.ProjectItem projectItem)
        {
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

        public static string GetProjectTypeGuids(this EnvDTE.Project proj)
        {
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

        private static T? GetService<T>(object serviceProvider) where T : class
        {
            return (T?)GetService((IServiceProvider)serviceProvider, typeof(T).GUID);
        }

        private static object? GetService(IServiceProvider serviceProvider, Guid guid)
        {
            var hr = serviceProvider.QueryService(guid, guid, out var serviceHandle);

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

        public static string? TryGetFileName(this EnvDTE.ProjectItem projectItem)
        {
            try
            {
                // some items report a file count > 0 but don't return a file name!
                if (projectItem.FileCount > 0)
                {
                    return projectItem.FileNames[0];
                }
            }
            catch (Exception)
            {
                // s.a.
            }

            return null;
        }

        public static XDocument GetXmlContent(this EnvDTE.ProjectItem projectItem, LoadOptions loadOptions)
        {
            return XDocument.Parse(projectItem.GetContent(), loadOptions);
        }

        public static string GetContent(this EnvDTE.ProjectItem projectItem)
        {
            try
            {
                if (!projectItem.IsOpen)
                    projectItem.Open();

                var document = projectItem.Document;

                if (document != null)
                {
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

        private static string GetContent(EnvDTE.TextDocument document)
        {
            return document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
        }

        public static void SetContent(this EnvDTE.ProjectItem projectItem, string text)
        {
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

        private static void SetContent(EnvDTE.Document document, string? text)
        {
            var textDocument = (EnvDTE.TextDocument)document.Object("TextDocument");

            textDocument.StartPoint.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);
        }

        public static object? TryGetObject(this EnvDTE.Project? project)
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

        public static EnvDTE80.DTE2? TryGetDte(this EnvDTE.Project? project)
        {
            try
            {
                return project?.DTE as EnvDTE80.DTE2;
            }
            catch
            {
                return null;
            }
        }

        public static bool GetCopyLocal(this VSLangProj.Reference? reference)
        {
            if (reference == null)
                return false;

            try
            {
                return reference.CopyLocal || (reference.ContainingProject != null);
            }
            catch
            {
                return false;
            }
        }
    }
}
