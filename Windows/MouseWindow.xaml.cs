using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnPop
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MouseWindow : Window
    {
        public Point ClickPosition { get; set; }
        public string RelativePosition { get; set; }
        public string ButtonId { get; set; }

        public MouseWindow(string senderButtonId)
        {
            InitializeComponent();
            ButtonId = senderButtonId;
        }

        public bool Completed = false;
        private void MouseWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    Process process = new();
                    // Try to get process
                    try
                    {
                        process = Logic.FirstProcessByString(Logic.TargetProcess);
                    }
                    catch
                    {
                        TextBlock_Display.Text = "Please open Encompass";
                        TextBlock_Display.Foreground = Brushes.Red;
                        return;
                    }

                    // Remove this event method from the window to prevent it from being fired multiple times
                    KeyDown -= MouseWindow_KeyDown;
                    
                    // Get window's bounds
                    Logic.WindowManipulation.Rect WindowRect = new();
                    Logic.WindowManipulation.GetWindowRect(process.MainWindowHandle, ref WindowRect);

                    // Get cursor position relative to the window
                    System.Drawing.Point cPos = System.Windows.Forms.Cursor.Position;
                    ClickPosition = new(cPos.X - WindowRect.Left, cPos.Y - WindowRect.Top);

                    // Display position buttons
                    TextBlock_Display.Text = "Select the corner to click relative to";
                    TextBlock_Display.Foreground = Brushes.Black;
                    Grid_Buttons.Visibility = Visibility.Visible;
                    Width = 410;
                    Height = 125;
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        private void Position_Click(object sender, RoutedEventArgs e)
        {
            RelativePosition = ((Button)sender).Tag.ToString();
            Completed = true;
            Close();
        }
    }
}
