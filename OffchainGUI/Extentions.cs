using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OffchainGUI
{
    public static class Extentions
    {
        public static string GetTextBoxValueSafely(this TextBox textBox, Dispatcher dispatcher)
        {
            return dispatcher.Invoke(new Func<string>(() => { return textBox.Text; }));
        }

        public static void SetTextBoxValueSafely(this TextBox textBox, Dispatcher dispatcher, string value)
        {
            string localValue = value;
            Action<string> dispatcherAction = new Action<string>(str => textBox.Text = str);
            dispatcher.Invoke(dispatcherAction, localValue);
        }

        public static object GetComboBoxSelectedItemSafely(this ComboBox comboBox, Dispatcher dispatcher)
        {
            return dispatcher.Invoke(new Func<object>(() => { return comboBox.SelectedItem; }));
        }

        public static string GetComboBoxSelectedValueContentSafely(this ComboBox comboBox, Dispatcher dispatcher)
        {
            return dispatcher.Invoke(new Func<string>(() => { return (comboBox.SelectedValue as ComboBoxItem).Content.ToString(); }));
        }
    }
}
