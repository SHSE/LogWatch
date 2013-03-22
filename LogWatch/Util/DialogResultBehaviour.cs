using System;
using System.Windows;
using System.Windows.Interactivity;

namespace LogWatch.Util {
    public class DialogResultBehaviour : Behavior<Window> {
        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.Register(
            "DialogResult", typeof (bool?), typeof (DialogResultBehaviour), new PropertyMetadata(OnDialogResultChanged));

        private bool isSourceInitialized;

        public DialogResultBehaviour() {
            this.CloseDialogWhenDialogResultHasBeenSet = true;
        }

        public bool? DialogResult {
            get { return (bool?) this.GetValue(DialogResultProperty); }
            set { this.SetValue(DialogResultProperty, value); }
        }

        public bool CloseDialogWhenDialogResultHasBeenSet { get; set; }

        protected override void OnAttached() {
            base.OnAttached();
            this.AssociatedObject.SourceInitialized += this.OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e) {
            this.AssociatedObject.DialogResult = this.DialogResult;
            this.isSourceInitialized = true;
            this.TryCloseDialog();
        }

        private void TryCloseDialog() {
            if (this.CloseDialogWhenDialogResultHasBeenSet && this.DialogResult != null)
                this.AssociatedObject.Close();
        }

        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((DialogResultBehaviour) d).OnDialogResultChanged();
        }

        private void OnDialogResultChanged() {
            if (this.isSourceInitialized) {
                this.AssociatedObject.DialogResult = this.DialogResult;
                this.TryCloseDialog();
            }
        }
    }
}