using System.Collections;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace LogWatch.Features.Records {
    public class SelectItemByIndexAction : TriggerAction<ListView> {
        protected override void Invoke(object parameter) {
            var eventArgs = parameter as GoToIndexEventArgs;

            if (eventArgs != null) {
                var collection = (IList)this.AssociatedObject.ItemsSource;
                var item = collection[eventArgs.Index];

                this.AssociatedObject.SelectedItem = item;
                this.AssociatedObject.ScrollIntoView(item);
            }
        }
    }
}