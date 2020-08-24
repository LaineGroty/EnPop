using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WindowsInput.Native;
using WindowsInput;

namespace EnPop
{
    /// <summary>
    /// Interaction logic for AdvancedWindow.xaml
    /// </summary>
    public partial class AdvancedWindow : Window
    {
        // Main ---------------------------------------------------------------------------------------------
        public string LoadInstructionTarget = "";
        private static List<FormattedInstruction> formattedInstructions = new();

        public AdvancedWindow()
        {
            InitializeComponent();
            UpdateDataGrid();
            Closing += MainWindow.AdvancedWindow_Closing;
        }

        // Button clicks ------------------------------------------------------------------------------------
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Button_Start.IsEnabled = false;
            Button_Stop.IsEnabled = true;
            AdvWindow.KeyDown += AdvancedWindow_KeyDown;
            AdvWindow.KeyUp += AdvancedWindow_KeyUp;
            DataGrid_Instructions.Visibility = Visibility.Visible;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Button_Start.IsEnabled = true;
            Button_Stop.IsEnabled = false;
            AdvWindow.KeyDown -= AdvancedWindow_KeyDown;
            AdvWindow.KeyUp -= AdvancedWindow_KeyUp;
            UpdateDataGrid();
        }

        private void AddClick_Click(object sender, RoutedEventArgs e)
        {
            formattedInstructions.Add(new()
            {
                Type = "click,tl",
                X = "0",
                Y = "0" 
            });
            UpdateDataGrid();
        }

        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            formattedInstructions.Add(new Instruction("text").Format());
            UpdateDataGrid();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            formattedInstructions = new List<FormattedInstruction>();
            UpdateDataGrid();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // From https://stackoverflow.com/questions/5622854/how-do-i-show-a-save-as-dialog-in-wpf
            Microsoft.Win32.SaveFileDialog dlg = new();
            dlg.FileName = "custominput"; // Default file name
            dlg.DefaultExt = ".epis"; // Default file extension
            dlg.Filter = "EnPop Instruction Sets (*.epis)|*.epis"; // Filter files by extension

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                List<Instruction> instructions = formattedInstructions.Select(i => i.ToInstruction()).ToList();
                File.WriteAllText(dlg.FileName, Logic.ParseInstructionsOut(instructions));
            }
        }

        private void SetPosition_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)e.Source;
            MouseWindow w = new(button.Tag.ToString());
            w.Closing += MouseWindow_Closing;
            Logic.OpenWindow(w);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new();
            dlg.DefaultExt = ".epis";
            dlg.Filter = "EPIS Files (*.epis)|*.epis";

            bool? result = dlg.ShowDialog();
            if (result == true)
                TextBox_File.Text = dlg.FileName;
        }

        // Methods and statics ------------------------------------------------------------------------------
        private void UpdateDataGrid()
        {
            formattedInstructions = Logic.CondenseInstructions(formattedInstructions);

            // Set display properties depending on whether instructions exist
            bool hasElements = formattedInstructions.Count > 0;
            DataGrid_Instructions.Visibility = (Visibility)(hasElements ? 0 : 2);
            Button_Reset.IsEnabled = hasElements;
            Button_Save.IsEnabled = hasElements;

            // Check if any instructions are click
            if(formattedInstructions.Any(i => i.Type.Contains("click")))
            {
                Column_Positions.Visibility = Visibility.Visible;
                for (int i = 0; i < formattedInstructions.Count; ++i)
                    // Check if current instruction is click
                    if (formattedInstructions[i].Type.Contains("click"))
                    {
                        // Generate a new button with a unique id
                        Button button = new ButtonPositionSetter();
                        button.Click += SetPosition_Click;
                        // Set last cell in current row to generated button
                        DataGrid_Instructions.GetCell(i, 3).Content = button;
                        formattedInstructions[i].Id = button.Tag.ToString();
                    }
            }
            // Collapse button column if no click instructions exist
            else Column_Positions.Visibility = Visibility.Collapsed;
            
            // Refresh grid source
            DataGrid_Instructions.ItemsSource = formattedInstructions;
        }

        // Events -------------------------------------------------------------------------------------------
        private static List<VirtualKeyCode> keysDown = new();
        private void AdvancedWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Avoid duplicate keypresses
            if (e.IsRepeat) return;
            InputSimulator input = new();
            // Iterate on all VirtualKeyCodes. If key is down, add it to keysDown. This is a *very* bad way of doing this.
            for (int i = 1; i < 255; ++i)
            {
                VirtualKeyCode cKey = (VirtualKeyCode)i;
                if (input.InputDeviceState.IsKeyDown(cKey) && !keysDown.Contains(cKey))
                {
                    string keyName = Enum.GetName(typeof(VirtualKeyCode), i);
                    formattedInstructions.Add(new("keydown", keyName));
                    keysDown.Add(cKey);
                }
            }
            UpdateDataGrid();
        }

        private void AdvancedWindow_KeyUp(object sender, KeyEventArgs e)
        {
            InputSimulator input = new();
            // Iterate on keys down, when a key is no longer down, add a keyup instruction
            for (int i = keysDown.Count - 1; i >= 0; --i)
            {
                VirtualKeyCode key = keysDown[i];
                if (input.InputDeviceState.IsKeyUp(key))
                {
                    string keyName = Enum.GetName(typeof(VirtualKeyCode), key);
                    formattedInstructions.Add(new("keyup", keyName));
                    keysDown.Remove(key);
                }
            }
            UpdateDataGrid();
        }

        private void MouseWindow_Closing(object sender, EventArgs e)
        {
            MouseWindow w = (MouseWindow)sender;
            if (w.Completed)
                // Find and set matching instruction's X and Y to the MouseWindow's selected X and Y
                for (int i = 0; i < formattedInstructions.Count; ++i)
                    if(w.ButtonId == formattedInstructions[i].Id)
                    {
                        formattedInstructions[i].Type = $"click,{w.RelativePosition}";
                        formattedInstructions[i].X = w.ClickPosition.X.ToString();
                        formattedInstructions[i].Y = w.ClickPosition.Y.ToString();
                        UpdateDataGrid();
                        break;
                    }
        }
    }

    class ButtonPositionSetter : Button
    {
        private int GenerateId() =>
            (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        public ButtonPositionSetter()
        {
            Tag = GenerateId();
            Content = "Set position";
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    // Visual Studio says this has no references, but it is needed for the "Set position" buttons
    static class CellManipulator
    {
        // From https://stackoverflow.com/questions/12164079/change-datagrid-cell-value-programmatically-in-wpf
        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default;
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                    child = GetVisualChild<T>(v);
                else
                    break;
            }
            return child;
        }

        public static DataGridRow GetSelectedRow(this DataGrid grid)
        {
            return (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem);
        }
        public static DataGridRow GetRow(this DataGrid grid, int index)
        {
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // May be virtualized, bring into view and try again.
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.Items[index]);
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static DataGridCell GetCell(this DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                return cell;
            }
            return null;
        }

        public static DataGridCell GetCell(this DataGrid grid, int row, int column)
        {
            DataGridRow rowContainer = grid.GetRow(row);
            return grid.GetCell(rowContainer, column);
        }
    }
}
