namespace tomenglertde.Wax
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    using DataGridExtensions;

    using JetBrains.Annotations;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {
        [NotNull]
        private readonly EnvDTE.DTE _dte;

        public MainView([NotNull] EnvDTE.DTE dte)
        {
            References.Resolve(this);

            _dte = dte;

            InitializeComponent();

            Loaded += Self_Loaded;
        }

        [CanBeNull]
        internal MainViewModel ViewModel
        {
            get => DataContext as MainViewModel;
            set => DataContext = value;
        }

        private void Self_Loaded([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            var viewModel = ViewModel;

            var solution = _dte.Solution;

            if ((solution == null) || (viewModel == null) || (viewModel.Solution.FullName != solution.FullName) || (viewModel.HasExternalChanges) || !viewModel.Solution.Projects.Any())
            {
                Refresh(solution);
            }
        }

        private void Refresh_Click([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            Refresh(_dte.Solution);
        }

        private void Refresh([CanBeNull] EnvDTE.Solution solution)
        {
            if (solution == null)
            {
                ViewModel = null;
                return;
            }

            try
            {
                ViewModel = new MainViewModel(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading: " + ex);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property != DataContextProperty)
                return;

            if ((!(e.OldValue is MainViewModel oldViewModel)) || (!(e.NewValue is MainViewModel newViewModel)))
                return;

            var oldSelectedProject = newViewModel.Solution.WixProjects.FirstOrDefault(p => p.Equals(oldViewModel.SelectedWixProject));

            if (oldSelectedProject == null)
                return;

            newViewModel.SelectedWixProject = oldSelectedProject;
        }

        private void SetupProjectListBox_Loaded([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;

            if ((listBox.Items.Count == 1) && (listBox.SelectedIndex == -1))
            {
                listBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Assemblies only referenced via reflection (XAML) can cause problems at runtime, sometimes they are not correctly installed
        /// by the VSIX installer. Add some code references to avoid this problem by forcing the assemblies to be loaded before the XAML is loaded.
        /// </summary>
        static class References
        {
            private static readonly DependencyProperty HardReferenceToDgx = DataGridFilterColumn.FilterProperty;

            public static void Resolve([NotNull] DependencyObject view)
            {
                if (HardReferenceToDgx == null) // just use this to avoid warnings...
                {
                    Trace.WriteLine("HardReferenceToDgx failed");
                }

                Interaction.GetBehaviors(view);
            }
        }
    }
}