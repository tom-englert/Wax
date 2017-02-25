namespace tomenglertde.Wax
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls.Primitives;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.Wax.Model.VisualStudio;

    using TomsToolbox.Wpf;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("ba4ab97f-d341-4b14-b8c9-3cba5e401a5f")]
    public class ToolWindow : ToolWindowPane
    {
        private EnvDTE.DTE _dte;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public ToolWindow()
            : base(null)
        {
            // Just to reference something to force load of referenced libraries.
            BindingErrorTracer.Start(msg => { Debug.WriteLine(msg); });

            Caption = Resources.ToolWindowTitle;

            BitmapResourceID = 301;
            BitmapIndex = 1;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            EventManager.RegisterClassHandler(typeof(MainView), ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));

            var t = typeof(Solution);
            if (t.GetMembers().Length == 0)
            {
                // Just to make sure the assembly is loaded, loading it dynamically from XAML may not work!
            }

            _dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));

            Contract.Assume(_dte != null);

            Content = new MainView(_dte);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            var button = source.TryFindAncestorOrSelf<ButtonBase>();
            if (button == null)
                return;

            var url = source.Tag as string;
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

            CreateWebBrowser(url);
        }


        [Localizable(false)]
        private void CreateWebBrowser([NotNull] string url)
        {
            Contract.Requires(url != null);

            var webBrowsingService = (IVsWebBrowsingService)GetService(typeof(SVsWebBrowsingService));
            if (webBrowsingService != null)
            {
                IVsWindowFrame pFrame;
                var hr = webBrowsingService.Navigate(url, (uint)__VSWBNAVIGATEFLAGS.VSNWB_WebURLOnly, out pFrame);
                if (ErrorHandler.Succeeded(hr) && (pFrame != null))
                {
                    hr = pFrame.Show();
                    if (ErrorHandler.Succeeded(hr))
                        return;
                }
            }

            Process.Start(url);
        }
    }
}
