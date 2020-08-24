using System;
using System.Diagnostics;
using WindowsInput.Native;

namespace EnPop
{
    public class Instruction
    {
        public string Type { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public string ClickRelativeTo { get; set; }

        public Instruction() { }

        public Instruction(string type) =>
            Type = type;

        public Instruction(string type, int? x)
        {
            Type = type;
            X = x;
        }

        public Instruction(string type, int? x, int? y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public Instruction(string type, int? x, int? y, string clickRelativeTo)
        {
            Type = type;
            X = x;
            Y = y;
            ClickRelativeTo = clickRelativeTo;
        }

        public FormattedInstruction Format() =>
            new FormattedInstruction()
            {
                Type = Type + (ClickRelativeTo == "" ? ",tl" : ClickRelativeTo),
                X = Type.Contains("key") ? Enum.GetName(typeof(VirtualKeyCode), X) : X.ToString(),
                Y = Y.ToString()
            };

        public override string ToString() => Type switch
        {
            "keydown" => $"d{X}",
            "keyup" => $"u{X}",
            "keystroke" => $"k{X}",
            "text" => $"t",
            "click" => $"c,{X},{Y},{(ClickRelativeTo == "" ? "tl" : ClickRelativeTo)}",
            _ => null,
        };
    }

    public class FormattedInstruction
    {
        public string Type { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string Id { get; set; }

        public FormattedInstruction() { }

        public FormattedInstruction(string type) =>
            Type = type;

        public FormattedInstruction(string type, string x)
        {
            Type = type;
            X = x;
        }

        public FormattedInstruction(string type, string x, string y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public Instruction ToInstruction()
        {
            Instruction instruction = new();
            if (Type.Contains("click"))
            {
                // If type has info for relative position, use it; otherwise, set the relative position to top-left
                string clickRel = Type.Length == 7 ? Type.Substring(5) : "tl";
                return new ("click", Convert.ToInt32(X), Convert.ToInt32(Y), clickRel);
            }
            else if (Type == "text")
                instruction.Type = "text";
            else // The only types remaining are keys
            {
                instruction.Type = Type.ToLower();
                instruction.X = (int)Enum.Parse(typeof(VirtualKeyCode), X.ToUpper());
            }
            return instruction;
        }
    }
}
