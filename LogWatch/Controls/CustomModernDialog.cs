using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;

namespace LogWatch.Controls {
    public class CustomModernDialog {
        public static string ShowMessage(string text, string title, Window owner, params ButtonDef[] buttons) {
            string result = null;

            var dialog = new ModernDialog {
                Title = title,
                Content = new BBCodeBlock {BBCode = text, Margin = new Thickness(0, 0, 0, 8)},
                MinHeight = 0,
                MinWidth = 0,
                MaxHeight = 480,
                MaxWidth = 640,
                Owner = owner
            };

            dialog.Buttons = buttons.Select(def => CreateButton(def, () => {
                result = def.Name;
                dialog.Close();
            }));

            dialog.ShowDialog();

            return result;
        }

        private static Button CreateButton(ButtonDef def, Action callback) {
            var button = new Button {
                Content = def.Text,
                IsDefault = def.IsDefault,
                IsCancel = def.IsCancel,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
            };

            RoutedEventHandler handler = null;

            handler = (sender, args) => {
                button.Click -= handler;
                callback();
            };

            button.Click += handler;

            return button;
        }
    }

    public sealed class ButtonDef {
        public ButtonDef(string name, string text, bool isDefault = false, bool isCancel = false) {
            this.Text = text;
            this.IsDefault = isDefault;
            this.IsCancel = isCancel;
            this.Name = name;
        }

        public static readonly ButtonDef Cancel = new ButtonDef("cancel", "cancel", isCancel: true);

        public string Name { get; private set; }
        public string Text { get; private set; }
        public bool IsDefault { get; private set; }
        public bool IsCancel { get; private set; }
    }
}