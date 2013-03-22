using System.Windows.Controls;
using System.Windows.Interactivity;

namespace LogWatch.Features.Records {
    public class SelectItemByIndexAction : TriggerAction<ListView> {
        protected override void Invoke(object parameter) {
            var eventArgs = parameter as GoToIndexEventArgs;

            if (eventArgs != null) {
                this.AssociatedObject.SelectedIndex = eventArgs.Index;
                this.AssociatedObject.ScrollIntoView(this.AssociatedObject.SelectedItem);
            }
        }
    }
}