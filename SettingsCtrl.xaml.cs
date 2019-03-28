using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NoteCountRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        Settings settings;

        bool initialised = false;
        public SettingsCtrl(Settings settings) : base()
        {
            this.settings = settings;
            InitializeComponent();
            foreach (var font in System.Drawing.FontFamily.Families)
            {
                var dock = new DockPanel();
                dock.Children.Add(new Label()
                {
                    Content = font.Name,
                    Padding = new Thickness(2)
                });
                dock.Children.Add(new Label()
                {
                    Content = "AaBbCc123",
                    FontFamily = new FontFamily(font.Name),
                    Padding = new Thickness(2),
                    VerticalContentAlignment = VerticalAlignment.Center
                });
                var item = new ComboBoxItem()
                {
                    Content = dock
                };
                item.Tag = font.Name;
                fontSelect.Items.Add(item);
            }
            foreach(var i in fontSelect.Items)
            {
                if((string)((ComboBoxItem)i).Tag == settings.fontName)
                {
                    fontSelect.SelectedItem = i;
                    break;
                }
            }
            fontSize.Value = settings.fontSize;
            textTemplate.Text = settings.text;
            initialised = true;
        }

        private void AlignSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialised) return;
            settings.textAlignment = (Alignments)alignSelect.SelectedIndex;
        }

        private void FontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!initialised) return;
            settings.fontSize = (int)fontSize.Value;
        }

        private void FontSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialised) return;
            settings.fontName = (string)((ComboBoxItem)fontSelect.SelectedItem).Tag;
        }

        private void TextTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialised) return;
            settings.text = textTemplate.Text;
        }
    }
}
