using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ModelessForms.IssuesManager.ViewModels;
using Shared.Theme;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>Edits the <see cref="TextField.Value"/> of each field. OK keeps the values.</summary>
    public partial class TextFieldsDialog : Window
    {
        private bool _focused;

        public TextFieldsDialog(string title, IReadOnlyList<TextField> fields)
        {
            ThemeManager.Apply(this);
            InitializeComponent();
            Title = title;
            Fields.ItemsSource = fields;
        }

        private void OnFieldLoaded(object sender, RoutedEventArgs e)
        {
            if (_focused) return;
            _focused = true; // the first field gets the keyboard focus
            var box = (TextBox)sender;
            box.Focus();
            box.SelectAll();
        }

        private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
