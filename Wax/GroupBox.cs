namespace tomenglertde.Wax
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class GroupBox : HeaderedContentControl
    {
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
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
        [NotNull]
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
        [NotNull]
        public static readonly DependencyProperty OrdinalProperty =
            DependencyProperty.Register("Ordinal", typeof(int), typeof(GroupBox));
    }
}
