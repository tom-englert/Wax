namespace tomenglertde.Wax
{
    using System.Windows;
    using System.Windows.Controls;

    using TomsToolbox.Desktop;

    public class GroupBox : HeaderedContentControl
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static GroupBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupBox), new FrameworkPropertyMetadata(typeof(GroupBox)));
        }


        public bool IsOk
        {
            get { return this.GetValue<bool>(IsOkProperty); }
            set { SetValue(IsOkProperty, value); }
        }
        /// <summary>
        /// Identifies the IsOk dependency property
        /// </summary>
        public static readonly DependencyProperty IsOkProperty =
            DependencyProperty.Register("IsOk", typeof(bool), typeof(GroupBox));


        public int Ordinal
        {
            get { return this.GetValue<int>(OrdinalProperty); }
            set { SetValue(OrdinalProperty, value); }
        }
        /// <summary>
        /// Identifies the Ordinal dependency property
        /// </summary>
        public static readonly DependencyProperty OrdinalProperty =
            DependencyProperty.Register("Ordinal", typeof(int), typeof(GroupBox));
    }
}
