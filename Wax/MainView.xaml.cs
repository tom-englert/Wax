namespace tomenglertde.Wax
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

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
    }
}