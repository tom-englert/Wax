namespace tomenglertde.Wax
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    using TomsToolbox.Wpf;

    public class GroupBox : HeaderedContentControl
    {
        static GroupBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupBox), new FrameworkPropertyMetadata(typeof(GroupBox)));
        }


        public bool IsOk
        {
            get => this.GetValue<bool>(IsOkProperty);
            set => SetValue(IsOkProperty, value);
        }
        /// <summary>
        /// Identifies the IsOk dependency property
        /// </summary>
        public static readonly DependencyProperty IsOkProperty =
            DependencyProperty.Register("IsOk", typeof(bool), typeof(GroupBox));


        public int Ordinal
        {
            get => this.GetValue<int>(OrdinalProperty);
            set => SetValue(OrdinalProperty, value);
        }
        /// <summary>
        /// Identifies the Ordinal dependency property
        /// </summary>
        public static readonly DependencyProperty OrdinalProperty =
            DependencyProperty.Register("Ordinal", typeof(int), typeof(GroupBox));
    }
}
