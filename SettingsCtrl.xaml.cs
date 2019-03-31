using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;

namespace NoteCountRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        Settings settings;

        string defText = "Notes: {nc} / {tn}\nBPM: {bpm}\nNPS: {nps}\nPPQ: {ppq}\nPolyphony: {plph}\nSeconds: {seconds}\nTime: {time}\nTicks: {ticks}";

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
            Reload();
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

        List<string> templateStrings = new List<string>();
        void Reload()
        {
            if (!Directory.Exists("Plugins/Assets/NoteCounter/Templates"))
            {
                Directory.CreateDirectory("Plugins/Assets/NoteCounter/Templates");
            }
            try
            {
                File.WriteAllText("Plugins/Assets/NoteCounter/Templates/full.txt", defText);
            }
            catch { }
            var files = Directory.GetFiles("Plugins/Assets/NoteCounter/Templates").Where(f => f.EndsWith(".txt"));
            templateStrings.Clear();
            templates.Items.Clear();
            foreach(var f in files)
            {
                string text = File.ReadAllText(f);
                templateStrings.Add(text);
                templates.Items.Add(new ComboBoxItem() { Content = Path.GetFileNameWithoutExtension(f) });
            }
            foreach(var i in templates.Items)
            {
                if((string)((ComboBoxItem)i).Content == "full")
                {
                    templates.SelectedItem = i;
                    break;
                }
            }
        }

        private void Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                textTemplate.Text = templateStrings[templates.SelectedIndex];
            }
            catch { }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }
    }
}
