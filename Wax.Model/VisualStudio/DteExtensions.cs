namespace tomenglertde.Wax.Model.VisualStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;
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
        public static IEnumerable<EnvDTE.Project> GetProjects(this EnvDTE.Solution solution)
        {
            Contract.Requires(solution != null);
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.Project>>() != null);

            var items = new List<EnvDTE.Project>();

            var projects = solution.Projects;
            Contract.Assume(projects != null);

            for (var i = 1; i <= projects.Count; i++)
            {
                try
                {
                    var project = projects.Item(i);
                    Contract.Assume(project != null);

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

        private static void GetSubProjects(this IEnumerable projectItems, ICollection<EnvDTE.Project> items)
        {
            Contract.Requires(items != null);

            if (projectItems == null)
                return;

            foreach (var projectItem in projectItems.OfType<EnvDTE.ProjectItem>())
            {
                projectItem.GetSubProjects(items);
            }
        }

        private static void GetSubProjects(this EnvDTE.ProjectItem projectItem, ICollection<EnvDTE.Project> items)
        {
            Contract.Requires(items != null);

            if (projectItem == null)
                return;

            var subProject = projectItem.SubProject;

            if (subProject == null)
                return;

            items.Add(subProject);

            subProject.ProjectItems.GetSubProjects(items);
        }

        public static IEnumerable<EnvDTE.ProjectItem> GetAllProjectItems(this EnvDTE.Project project)
        {
            if (project.ProjectItems == null)
                yield break;

            foreach (var item in project.ProjectItems.OfType<EnvDTE.ProjectItem>())
            {
                yield return item;

                foreach (var subItem in GetProjectItems(item))
                {
                    yield return subItem;
                }
            }
        }

        private static IEnumerable<EnvDTE.ProjectItem> GetProjectItems(EnvDTE.ProjectItem projectItem)
        {
            if (projectItem.ProjectItems == null)
                yield break;

            foreach (var item in projectItem.ProjectItems.OfType<EnvDTE.ProjectItem>())
            {
                yield return item;

                foreach (var subItem in GetProjectItems(item))
                {
                    yield return subItem;
                }
            }
        }

        public static string GetProjectTypeGuids(this EnvDTE.Project proj)
        {
            Contract.Requires(proj != null);
            Contract.Ensures(Contract.Result<string>() != null);

            try
            {
                Contract.Assume(proj.DTE != null);
                var solution = GetService<IVsSolution>(proj.DTE);

                if (solution == null)
                    return string.Empty;

                IVsHierarchy hierarchy;
                var result = solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

                if (result == 0)
                {
                    var aggregatableProject = hierarchy as IVsAggregatableProjectCorrected;

                    if (aggregatableProject != null)
                    {
                        string projectTypeGuids;
                        result = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);

                        if ((result == 0) && (projectTypeGuids != null))
                            return projectTypeGuids;

                    }
                }
            }
            catch (ExternalException)
            {
            }

            return string.Empty;
        }

        private static T GetService<T>(object serviceProvider) where T : class
        {
            Contract.Requires(serviceProvider != null);

            return (T)GetService((IServiceProvider)serviceProvider, typeof(T).GUID);
        }

        private static object GetService(IServiceProvider serviceProvider, Guid guid)
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

        public static string TryGetFileName(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            try
            {
                // some items report a file count > 0 but don't return a file name!
                if (projectItem.FileCount > 0)
                {
                    return projectItem.FileNames[0];
                }
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        public static XDocument GetXmlContent(this EnvDTE.ProjectItem projectItem, LoadOptions loadOptions)
        {
            Contract.Requires(projectItem != null);
            Contract.Ensures(Contract.Result<XDocument>() != null);

            return XDocument.Parse(projectItem.GetContent(), loadOptions);
        }

        public static string GetContent(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);
            Contract.Ensures(Contract.Result<string>() != null);

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

        [ContractVerification(false)]
        private static string GetContent(EnvDTE.TextDocument document)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
        }

        public static void SetContent(this EnvDTE.ProjectItem projectItem, string text)
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

        [ContractVerification(false)]
        private static void SetContent(EnvDTE.Document document, string text)
        {
            if (!document.ActiveWindow.Visible)
            {
                var activeWindow = document.DTE.ActiveWindow;
                document.ActiveWindow.Visible = true;
                activeWindow.Activate();
            }

            var textDocument = (EnvDTE.TextDocument)document.Object("TextDocument");

            textDocument.StartPoint.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);

            // Must heavily delay formatting - does not work if called too early, especially if the window has been recently opened.
            EventHandler callback = (sender, e) =>
            {
                FormatSelection(textDocument.Selection);
                ((DispatcherTimer)sender).Stop();
            };

            new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Background, callback, Dispatcher.CurrentDispatcher);
        }

        public static void FormatSelection(EnvDTE.TextSelection selection)
        {
            Contract.Requires(selection != null);

            try
            {
                var selStart = selection.AnchorPoint;
                var selEnd = selection.ActivePoint;

                selection.SelectAll();
                selection.SmartFormat();

                selection.MoveToPoint(selStart);
                selection.MoveToPoint(selEnd, true);
            }
            catch
            {
            }
        }
    }
}
