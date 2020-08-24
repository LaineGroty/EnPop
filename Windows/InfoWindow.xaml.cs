using EnPop.defaults;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Windows;

namespace EnPop
{
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow() => InitializeComponent();

        private static readonly string WriteDir = $@"{Environment.CurrentDirectory}\EnPop-Defaults\";
        private static readonly Dictionary<string, string> TypeNames = new()
        {
            { "pcon", "preliminary-conditions" },
            { "lend", "lender-presets" }
        };

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            ResourceManager resourceMan = Defaults.ResourceManager;
            ResourceSet resources = resourceMan.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            // Create directories
            Directory.CreateDirectory(WriteDir);
            foreach(string folder in TypeNames.Values)
                Directory.CreateDirectory($@"{WriteDir}\{folder}");

            // Write files to directories
            foreach(DictionaryEntry entry in resources)
            {
                string[] fileNameSplit = entry.Key.ToString().Split('_');
                string type = TypeNames[fileNameSplit[0]];
                string path = $@"{WriteDir}\{type}\{fileNameSplit[1]}";
                File.WriteAllText($@"{path}.txt", entry.Value.ToString());
            }

            TextBlock_Done.Visibility = Visibility.Visible;
            Button_Open.Visibility = Visibility.Visible;
        }

        private void Open_Click(object sender, RoutedEventArgs e) => Process.Start("explorer", WriteDir);
    }
}
