using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LogWatch.Controls {
    public class IconToggleButton : ToggleButton {
        public static readonly DependencyProperty IconDataProperty = DependencyProperty.Register(
            "IconData", typeof (Geometry), typeof (IconToggleButton));

        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(
            "IconHeight", typeof (double), typeof (IconToggleButton), new PropertyMetadata(16D));

        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(
            "IconWidth", typeof (double), typeof (IconToggleButton), new PropertyMetadata(16D));

        public IconToggleButton() {
            this.DefaultStyleKey = typeof (IconToggleButton);
        }

        public Geometry IconData {
            get { return (Geometry) this.GetValue(IconDataProperty); }
            set { this.SetValue(IconDataProperty, value); }
        }

        public double IconHeight {
            get { return (double) this.GetValue(IconHeightProperty); }
            set { this.SetValue(IconHeightProperty, value); }
        }

        public double IconWidth {
            get { return (double) this.GetValue(IconWidthProperty); }
            set { this.SetValue(IconWidthProperty, value); }
        }
    }
}