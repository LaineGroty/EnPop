using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnPop
{
    class LoanParsing
    {
        /// <summary>
        /// Parses information from a file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lender">The name of the lender (derived from combobox option tag)</param>
        /// <returns></returns>
        public static List<string> GetFileDetails(string filePath, string lender) => lender switch
        {
            "caliber" => DetailsFromCaliber(filePath),
            "text" => DetailsFromTXT(filePath),
            _ => new(),
        };

        /// <summary>
        /// (Caliber Home Loans) Gets non-cleared elements in document at specified location
        /// </summary>
        /// <param name="filePath">The path of the file to be read</param>
        /// <returns>A list containing each condition found in a Caliber Home Loans file</returns>
        private static List<string> DetailsFromCaliber(string filePath)
        {
            List<string> fileLines = File.ReadAllLines(filePath).ToList();

            Regex matchCleared = new Regex(@"lblCompletedDate.+>(Cleared|Waived)");
            Regex matchDetailsStart = new Regex(@"lblDetails.+>(.+)");
            Regex matchDetailsEnd = new Regex(@"(.+)<\/SPAN><\/TD><\/TR>");

            List<string> output = new();
            for (int i = 0; i < fileLines.Count; ++i)
                if (matchCleared.IsMatch(fileLines[i]))
                    // Skip to next line matching matchDetailsEnd
                    i = fileLines.FindIndexOfPattern(matchDetailsEnd, i);
                // Check if line starts a detail block
                else if (matchDetailsStart.IsMatch(fileLines[i]))
                {
                    // Find the index of the end of the block
                    int blockEndIndex = fileLines.FindIndexOfPattern(matchDetailsEnd, i);
                    // Get the lines of the block
                    List<string> currentLines = fileLines.Skip(i)
                                                         .Take(blockEndIndex - i)
                                                         .ToList();
                    
                    // Trim HTML junk from first and last lines
                    int lastLine = currentLines.Count - 1;
                    currentLines[lastLine] = matchDetailsEnd.Match(currentLines[lastLine]).Groups[1].Value;
                    currentLines[0] = matchDetailsStart.Match(currentLines[0]).Groups[1].Value;

                    output.Add(currentLines.JoinBlock());
                }

            // Remove trailing junk text
            output = output.RemoveStringFromList("=20");
            output = output.RemoveStringFromList(" =");

            return output;
        }

        /// <summary>
        /// Reads in all lines, using each line as an element
        /// </summary>
        /// <param name="filePath">The path of the file to be read</param>
        /// <returns>A list containing each line of a file</returns>
        private static List<string> DetailsFromTXT(string filePath) =>
            File.ReadAllLines(filePath).ToList();
    }
    
    static class StringExtensions
    {
        /// <summary>
        /// Removes every occurance of a string from a list
        /// </summary>
        /// <param name="list">The list to be operated on</param>
        /// <param name="str">The string to be removed</param>
        public static List<string> RemoveStringFromList(this List<string> list, string str)
        {
            while (list.Any(x => x.Contains(str)))
                list = list.Select(x => x.Replace(str, "")).ToList();
            return list;
        }

        public static string JoinBlock(this List<string> block) =>
            // Decode the HTML present within
            WebUtility.HtmlDecode(
                // The combined elements in the list, separated by a space, where
                string.Join(" ",
                    // Each element is trimmed of its leading and trailing whitespace
                    block.Select(x => x.Trim())));

        /// <summary>
        /// Joins a List<List<string>> into a single List<string>, where each List<string> is composed of the nested List<string>. Intended for lists sentences broken into multiple strings.
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public static List<string> JoinBlocks(this List<List<string>> blocks) =>
            blocks.Select(x => x.JoinBlock()).ToList();

        /// <summary>
        /// Finds the index of a Regex pattern starting at the given index
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pattern">The Regex pattern to search for</param>
        /// <param name="startIndex">The index to start at. Can be left blank for zero.</param>
        /// <returns></returns>
        public static int FindIndexOfPattern(this List<string> lines, Regex pattern, int startIndex = 0)
        {
            for (int i = startIndex; i < lines.Count; ++i)
                if (pattern.IsMatch(lines[i]))
                    return i;
            return -1;
        }
    }
}
