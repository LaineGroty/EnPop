using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using Point = System.Drawing.Point;

namespace EnPop
{
    public static class Logic
    {
        public static class WindowManipulation
        {
            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
            public struct Rect
            {
                public int Left { get; set; }
                public int Top { get; set; }
                public int Right { get; set; }
                public int Bottom { get; set; }
            }
        }

        // Window information -------------------------------------------------------------------------------
        public static string TargetProcess = "encompass";
        private static WindowManipulation.Rect window = new();
        private static Dictionary<string, Point> corners = new()
        {
            { "tl", new(0, 0) },
            { "tr", new(0, 0) },
            { "bl", new(0, 0) },
            { "br", new(0, 0) }
        };
        private static readonly Dictionary<string, int[]> cMod = new()
        {
            { "tl", new int[] { 1, 1 } },
            { "tr", new int[] { -1, 1 } },
            { "bl", new int[] { 1, -1 } },
            { "br", new int[] { -1, -1 } }
        };
        // tl (x,y); tr (-x,y); bl (x,-y); br (-x,-y) 

        // Interaction information --------------------------------------------------------------------------
        private static int delayBetweenActions = 50;
        private const string defaultInstructions = "c430,130,tr/k13/t/k27";

        /// <summary>
        /// Enter data into Encompass process from given conditions
        /// </summary>
        /// <param name="process">The process to operate on</param>
        /// <param name="text">The list of text to be entered</param>
        /// <param name="instructionString">The string representing the instructions to follow</param>
        public static void EnterData(Process process, List<string> text, string instructionString)
        {
            // Write debug configuration file
            if(true)
            {
                string debugPath = @".\debug.txt";
                if(File.Exists(debugPath))
                {
                    string[] lines = File.ReadAllLines(defaultInstructions);
                    instructionString = lines[0];

                    if(lines.Length > 1)
                    {
                        bool isInt = int.TryParse(lines[1], out int readDelay);
                        if(isInt) delayBetweenActions = readDelay;
                    }
                }
                else File.WriteAllText(debugPath, instructionString);
            }

            // Bring window into focus
            WindowManipulation.ShowWindow(process.MainWindowHandle, 9);
            WindowManipulation.SetForegroundWindow(process.MainWindowHandle);

            // Get window details
            WindowManipulation.GetWindowRect(process.MainWindowHandle, ref window);
            SetCorners();

            // If instructionString is empty, replace it with defaultInstructions
            instructionString = instructionString != "" ? instructionString : defaultInstructions;

            // Enter data
            List<Instruction> instructions = ParseInstructionsIn(instructionString);
            InstructionLoop(text, instructions);
        }

        /// <summary>
        /// Follow given instruction loop to insert text
        /// </summary>
        /// <param name="text">The elements to insert</param>
        /// <param name="instructions">The instruction cycle to follow</param>
        private static void InstructionLoop(List<string> text, List<Instruction> instructions)
        {
            InputSimulator input = new();
            while(text.Count > 0)
                foreach(Instruction i in instructions)
                {
                    switch(i.Type)
                    {
                        case "keydown":
                            input.Keyboard.KeyDown((VirtualKeyCode)i.X);
                            break;
                        case "keyup":
                            input.Keyboard.KeyUp((VirtualKeyCode)i.X);
                            break;
                        case "keystroke":
                            input.Keyboard.KeyPress((VirtualKeyCode)i.X);
                            break;
                        case "click":
                            string rPos = i.ClickRelativeTo;
                            int x = (int)i.X * cMod[rPos][0];
                            int y = (int)i.Y * cMod[rPos][1];
                            Cursor.Position = AddPoints(corners[rPos], new(x, y));
                            input.Mouse.LeftButtonClick();
                            break;
                        case "text":
                            input.Keyboard.TextEntry(text[0]);
                            text.RemoveAt(0);
                            break;
                        default:
                            continue;
                    }
                    Thread.Sleep(delayBetweenActions);
                }
        }

        // Instruction parsing ------------------------------------------------------------------------------
        /// <summary>
        /// Parses a string representing inputs into a list of instructions
        /// </summary>
        /// <param name="instructionString">A string representing instructions</param>
        /// <returns>A list of instructions representing keyboard and mouse inputs</returns>
        public static List<Instruction> ParseInstructionsIn(string instructionString)
        {
            instructionString = instructionString.ToLower();
            string[] instructionSplit = instructionString.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            List<Instruction> instructions = new();
            foreach(string s in instructionSplit)
            {
                string instructionDetail = s.Substring(1);
                switch(s[0])
                {
                    case 'd':
                        instructions.Add(new("keydown", Convert.ToInt32(instructionDetail)));
                        break;
                    case 'u':
                        instructions.Add(new("keyup", Convert.ToInt32(instructionDetail)));
                        break;
                    case 'k':
                        instructions.Add(new("keystroke", Convert.ToInt32(instructionDetail)));
                        break;
                    case 't':
                        instructions.Add(new("text"));
                        break;
                    case 'c':
                        string[] clickSplit = instructionDetail.Split(',');
                        int[] clicks =
                        {
                            Convert.ToInt32(clickSplit[0]),
                            Convert.ToInt32(clickSplit[1])
                        };
                        instructions.Add(new("click", clicks[0], clicks[1], clickSplit[2]));
                        break;
                    default:
                        continue;
                }
            }
            return instructions;
        }

        /// <summary>
        /// Parses a list of instructions to a BitArray represting the instructions
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static string ParseInstructionsOut(List<Instruction> instructions)
        {
            instructions = CondenseInstructions(instructions);
            return string.Join("/", instructions.Select(i => i.ToString()));
        }

        // Helper methods -----------------------------------------------------------------------------------
        /// <summary>
        /// Adds the x and y valyes of two points together
        /// </summary>
        /// <param name="a">A point</param>
        /// <param name="b">A point</param>
        /// <returns>The sum of the x and y values of two points</returns>
        public static Point AddPoints(Point a, Point b) =>
            new(a.X + b.X, a.Y + b.Y);

        /// <summary>
        /// Sets values in corners dictionary based on the known window
        /// </summary>
        private static void SetCorners()
        {
            corners["tl"] = new(window.Left, window.Top);
            corners["tr"] = new(window.Right, window.Top);
            corners["bl"] = new(window.Left, window.Bottom);
            corners["br"] = new(window.Right, window.Bottom);
        }

        /// <summary>
        /// Opens the specified window. Will not open another instance of the window if an instance of it is already present.
        /// </summary>
        /// <param name="windowType">A window</param>
        public static void OpenWindow(Window windowType)
        {
            Window[] windows = System.Windows.Application.Current.Windows.Cast<Window>().Where(w => w.Title == windowType.Title).ToArray();
            Window window = windows.Length < 1 ? windowType : windows[0];

            window.Show();
            window.Activate();
        }

        /// <summary>
        /// Finds the first process whose name contains the given string
        /// </summary>
        /// <param name="processName"></param>
        public static Process FirstProcessByString(string processName) =>
            Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains(processName)).First();

        /// <summary>
        /// Condenses back-to-back "keyup"s and "keydown"s of the same key
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static List<Instruction> CondenseInstructions(List<Instruction> instructions)
        {
            for(int i = instructions.Count - 1; i > 0; --i)
            {
                Instruction c = instructions[i];
                Instruction p = instructions[i - 1];
                if(c.Type == "keyup" && p.Type == "keydown" && c.X == p.X)
                {
                    instructions.RemoveRange(i - 1, 2);
                    instructions.Insert(i - 1, new("keystroke", c.X));
                    --i;
                }
            }
            return instructions;
        }
        public static List<FormattedInstruction> CondenseInstructions(List<FormattedInstruction> instructions) =>
            CondenseInstructions(instructions.Select(i => i.ToInstruction())
                                             .ToList())
                                         .Select(i => i.Format())
                                         .ToList();
    }
}
