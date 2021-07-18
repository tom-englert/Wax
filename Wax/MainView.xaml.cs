namespace tomenglertde.Wax
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using JetBrains.Annotations;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {
        [NotNull]
        private readonly EnvDTE80.DTE2 _dte;

        public MainView([NotNull] EnvDTE80.DTE2 dte)
        {
            _dte = dte;

            InitializeComponent();

            Loaded += Self_Loaded;
        }

        public bool IsDarkTheme
        {
            get => (bool)GetValue(IsDarkThemeProperty);
            set => SetValue(IsDarkThemeProperty, value);
        }
        public static readonly DependencyProperty IsDarkThemeProperty = DependencyProperty.Register(
            "IsDarkTheme", typeof(bool), typeof(MainView), new PropertyMetadata(default(bool)));

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

            if (e.Property == DataContextProperty)
            {

                if ((!(e.OldValue is MainViewModel oldViewModel)) || (!(e.NewValue is MainViewModel newViewModel)))
                    return;

                var oldSelectedProject = newViewModel.Solution.WixProjects.FirstOrDefault(p => p.Equals(oldViewModel.SelectedWixProject));

                if (oldSelectedProject == null)
                    return;

                newViewModel.SelectedWixProject = oldSelectedProject;
                return;
            }

            if ((e.Property != ForegroundProperty) && (e.Property != BackgroundProperty))
                return;

            var foreground = ToGray((Foreground as SolidColorBrush)?.Color);
            var background = ToGray((Background as SolidColorBrush)?.Color);

            IsDarkTheme = background < foreground;
        }

        private static double ToGray(Color? color)
        {
            return color?.R * 0.21 + color?.G * 0.72 + color?.B * 0.07 ?? 0.0;
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