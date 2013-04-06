using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace LogWatch.Util {
    public sealed class CommandBindingTrigger : TriggerBase<FrameworkElement> {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof (ICommand), typeof (CommandBindingTrigger), new PropertyMetadata(OnCommandChanged));

        private CommandBinding commandBinding;

        public ICommand Command {
            get { return (ICommand) this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((CommandBindingTrigger) d).TryAddCommandBinding();
        }

        protected override void OnAttached() {
            base.OnAttached();
            this.TryAddCommandBinding();
        }

        protected override void OnDetaching() {
            base.OnDetaching();

            if (this.commandBinding != null)
                this.AssociatedObject.CommandBindings.Remove(this.commandBinding);
        }

        private void TryAddCommandBinding() {
            if (this.AssociatedObject == null)
                return;

            if (this.commandBinding != null)
                this.AssociatedObject.CommandBindings.Remove(this.commandBinding);

            this.commandBinding = new CommandBinding(this.Command, this.OnCommandExecuted);
            this.AssociatedObject.CommandBindings.Add(this.commandBinding);
        }

        private void OnCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            this.InvokeActions(null);
        }
    }
}