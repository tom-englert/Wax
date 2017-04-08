namespace tomenglertde.Wax
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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
            Contract.Requires(dte != null);

            References.Resolve(this);

            _dte = dte;

            InitializeComponent();

            Loaded += Self_Loaded;
        }

        internal MainViewModel ViewModel
        {
            get
            {
                return DataContext as MainViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        private void Self_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;

            var solution = _dte.Solution;

            Contract.Assume(solution != null);

            if ((viewModel == null) || (viewModel.Solution.FullName != solution.FullName) || (viewModel.HasExternalChanges) || !viewModel.Solution.Projects.Any())
            {
                Refresh();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            var solution = _dte.Solution;

            Contract.Assume(solution != null);

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

            var newViewModel = e.NewValue as MainViewModel;
            var oldViewModel = e.OldValue as MainViewModel;

            if ((oldViewModel == null) || (newViewModel == null))
                return;

            var oldSelectedProject = newViewModel.Solution.WixProjects.FirstOrDefault(p => p.Equals(oldViewModel.SelectedWixProject));

            if (oldSelectedProject == null)
                return;

            newViewModel.SelectedWixProject = oldSelectedProject;
        }

        private void SetupProjectListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;

            if ((listBox != null) && (listBox.Items.Count == 1) && (listBox.SelectedIndex == -1))
            {
                listBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Assemblies ony referenced via reflection (XAML) can cause problems at runtime, sometimes they are not correctly installed
        /// by the VSIX installer. Add some code references to avoid this problem by forcing the assemblies to be loaded before the XAML is loaded.
        /// </summary>
        static class References
        {
            private static readonly DependencyProperty HardReferenceToDgx = DataGridFilterColumn.FilterProperty;

            public static void Resolve(DependencyObject view)
            {
                if (HardReferenceToDgx == null) // just use this to avoid warnings...
                {
                    Trace.WriteLine("HardReferenceToDgx failed");
                }

                Interaction.GetBehaviors(view);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_dte != null);
        }
    }
}