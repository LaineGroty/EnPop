using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;

namespace EnPop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: add support for preliminary conditions (condo, HOA budget, master policy, current year financials)
        // Per-lender defaults

        // Main ---------------------------------------------------------------------------------------------
        private static readonly bool skipValidation = true;
        private static string UserInstructionLocation;
        public MainWindow() => InitializeComponent();

        // Button clicks ------------------------------------------------------------------------------------
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new();
            dlg.DefaultExt = ".mhtml";
            dlg.Filter = "MHTML Files (*.mhtml;*.mht)|*.mhtml;*.mht|TXT Files (*.txt)|*.txt";
            
            bool? result = dlg.ShowDialog();
            if (result == true)
                TextBox_File.Text = dlg.FileName;
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            // Verify user input
            int code = InputValidationCode();
            if (code != 0 && !skipValidation)
                TextBlock_Invalid.Text = "Invalid " + ValidationCodes[code];
            else
            {
                TextBlock_Invalid.Text = "";
                Button_Go.Content = "Confirm?";
                Button_Go.Click -= Go_Click;
                Button_Go.Click += Confirm_Click;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string lender = ((ComboBoxItem)ComboBox_Lenders.SelectedItem).Tag.ToString();

            List<string> details = LoanParsing.GetFileDetails(TextBox_File.Text, lender);
            Process process = Logic.FirstProcessByString(Logic.TargetProcess);
            // Check that the file has details
            if(details.Count == 0)
                TextBlock_Invalid.Text = "Invalid " + ValidationCodes[6];
            else
            {
                //AppWindow.Hide();
                string instructions = File.Exists(UserInstructionLocation) ? File.ReadAllText(UserInstructionLocation) : "";
                Logic.EnterData(process, details, instructions);
                //AppWindow.Show();
            }
        }

        private void Info_Click(object sender, RoutedEventArgs e) =>
            Logic.OpenWindow(new InfoWindow());

        private void Advanced_Click(object sender, RoutedEventArgs e) =>
            Logic.OpenWindow(new AdvancedWindow());

        // Methods & statics --------------------------------------------------------------------------
        /// <summary>
        /// Returns a number representing a unique set of invalid conditions
        /// </summary>
        /// <returns>0 when valid</returns>
        private int InputValidationCode()
        {
            int code = 0;
            // Check file
            if (!File.Exists(TextBox_File.Text))
                code += 1;
            // Count of processes where process's all-lowercase name is similar to the target process name
            int processCount = Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains(Logic.TargetProcess)).Count();
            if (processCount < 1) // No process
                code += 2;
            else if (processCount > 1) // Too many processes
                code += 4;
            
            return code;
        }

        private static readonly Dictionary<int, string> ValidationCodes = new()
        {
            { 0, "(unknown)" },
            { 1, "file" },
            { 2, "(NEP)" },
            { 3, "file, NEP" },
            { 4, "(MEP)" },
            { 5, "file, MEP" },
            { 6, "(ND)" }
        };

        // Events -------------------------------------------------------------------------------------------
        private void Button_Go_MouseLeave(object sender, RoutedEventArgs e)
        {
            Button_Go.Click += Go_Click;
            Button_Go.Click -= Confirm_Click;
            Button_Go.Content = "Go";
        }
        
        private void AppWindow_Closed(object sender, EventArgs e) =>
            Application.Current.Shutdown();

        public static void AdvancedWindow_Closing(object sender, EventArgs e) =>
            UserInstructionLocation = ((AdvancedWindow)sender).LoadInstructionTarget;
    }
}
