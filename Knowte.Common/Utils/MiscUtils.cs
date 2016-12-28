using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.IO;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;

namespace Knowte.Common.Utils
{
    public sealed class MiscUtils
    {
        public static void InitializeFiles()
        {
            string applicationFolder = SettingsClient.ApplicationFolder();

            // If the AppName directory doesn't exist, create it
            if (!Directory.Exists(applicationFolder))
            {
                Directory.CreateDirectory(applicationFolder);
            }

            // If the Notes directory doesn't exist, create it
            if (!Directory.Exists(System.IO.Path.Combine(applicationFolder, ApplicationPaths.NotesSubDirectory)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(applicationFolder, ApplicationPaths.NotesSubDirectory));
            }

            // If the Themes directory doesn't exist, create it
            if (!Directory.Exists(System.IO.Path.Combine(applicationFolder, ApplicationPaths.ColorSchemesSubDirectory)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(applicationFolder, ApplicationPaths.ColorSchemesSubDirectory));
            }
        }

        public static int CountOccurrences(string fullText, string findText)
        {
            int count = 0;
            if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(findText))
                return count;
            int pos = 0;
            do
            {
                pos = fullText.IndexOf(findText, pos);
                if (pos == -1)
                    return count;
                count += 1;
                pos += 1;
            } while (true);
        }

        public static string RemoveBullets(string input)
        {

            string retVal = "";
            string replaceStr = "";

            retVal = input.Replace("•\t", replaceStr);
            retVal = retVal.Replace("•", replaceStr);

            return retVal;
        }

        public static bool IsValidExportFile(string importFile)
        {

            bool retVal = false;

            string extension = System.IO.Path.GetExtension(importFile);

            // GetExtension(...) returns the extensio INCLUDING the dot
            if (extension.ToLower().Equals("." + Defaults.ExportFileExtension))
            {
                retVal = true;
            }

            return retVal;
        }

        public static bool IsUrl(string word)
        {

            bool retVal = false;

            Match m = Regex.Match(word, Defaults.UrlRegex, RegexOptions.IgnoreCase);

            if ((m.Success))
            {
                retVal = true;
            }

            return retVal;
        }

        public static bool IsMail(string word)
        {

            bool retVal = false;

            Match m = Regex.Match(word, Defaults.MailRegex, RegexOptions.IgnoreCase);

            if ((m.Success))
            {
                retVal = true;
            }

            return retVal;
        }

        // Helper that returns true when passed property applies to Hyperlink only.
        public static bool IsHyperlinkProperty(DependencyProperty dp)
        {
            return dp.Equals(Hyperlink.CommandProperty) || dp.Equals(Hyperlink.CommandParameterProperty) || dp.Equals(Hyperlink.CommandTargetProperty) || dp.Equals(Hyperlink.NavigateUriProperty) || dp.Equals(Hyperlink.TargetNameProperty);
        }

        // Helper that returns true if passed caretPosition and backspacePosition cross a hyperlink end boundary
        // (under the assumption that caretPosition and backSpacePosition are adjacent insertion positions).
        public static bool IsHyperlinkBoundaryCrossed(TextPointer caretPosition, TextPointer backspacePosition, ref Hyperlink backspacePositionHyperlink)
        {
            Hyperlink caretPositionHyperlink = GetHyperlinkAncestor(caretPosition);
            backspacePositionHyperlink = GetHyperlinkAncestor(backspacePosition);

            return (caretPositionHyperlink == null && backspacePositionHyperlink != null) || (caretPositionHyperlink != null && backspacePositionHyperlink != null && !caretPositionHyperlink.Equals(backspacePositionHyperlink));
        }

        // Helper that returns a hyperlink ancestor of passed position.
        public static Hyperlink GetHyperlinkAncestor(TextPointer position)
        {
            Inline parent = position.Parent as Inline;
            while (parent != null && !(parent is Hyperlink))
            {
                parent = parent.Parent as Inline;
            }

            return parent as Hyperlink;
        }

        // Helper that returns a word preceeding the passed position in its paragraph, 
        // wordStartPosition points to the start position of word.
        public static string GetPreceedingWordInParagraph(TextPointer position, ref TextPointer wordStartPosition)
        {
            wordStartPosition = null;
            string word = String.Empty;

            Paragraph paragraph = position.Paragraph;
            if (paragraph != null)
            {
                TextPointer navigator = position;
                while (navigator.CompareTo(paragraph.ContentStart) > 0)
                {
                    string runText = navigator.GetTextInRun(LogicalDirection.Backward);

                    if (runText.Contains(" "))
                    {
                        // Any globalized application would need more sophisticated word break testing.
                        int index = runText.LastIndexOf(" ");
                        word = runText.Substring(index + 1, runText.Length - index - 1) + word;
                        wordStartPosition = navigator.GetPositionAtOffset(-1 * (runText.Length - index - 1));
                        break; // TODO: might not be correct. Was : Exit While
                    }
                    else
                    {
                        wordStartPosition = navigator;
                        word = runText + word;
                    }
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Backward);
                }
            }

            return word;
        }

        public static string GetWordAtPointer(TextPointer textPointer)
        {
            return string.Join(string.Empty, GetWordCharactersBefore(textPointer), GetWordCharactersAfter(textPointer));
        }

        public static void ReplaceWordAtPointer(TextPointer textPointer, string replacementWord)
        {
            textPointer.DeleteTextInRun(-GetWordCharactersBefore(textPointer).Count());
            textPointer.DeleteTextInRun(GetWordCharactersAfter(textPointer).Count());

            textPointer.InsertTextInRun(replacementWord);
        }

        public static string GetWordCharactersBefore(TextPointer textPointer)
        {
            string backwards = textPointer.GetTextInRun(LogicalDirection.Backward);
            var wordCharactersBeforePointer = new string(backwards.Reverse().TakeWhile(c => !char.IsWhiteSpace(c)).Reverse().ToArray());

            return wordCharactersBeforePointer;
        }

        public static string GetWordCharactersAfter(TextPointer textPointer)
        {
            string fowards = textPointer.GetTextInRun(LogicalDirection.Forward);
            var wordCharactersAfterPointer = new string(fowards.TakeWhile(c => !char.IsWhiteSpace(c)).ToArray());

            return wordCharactersAfterPointer;
        }

        public static bool IsGuid(string text)
        {
            bool retVal = false;

            var guidRegex = new Regex("^(\\{){0,1}[0-9a-fA-F]{8}\\-" + "[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-" + "[0-9a-fA-F]{12}(\\}){0,1}$", RegexOptions.Compiled);

            if (guidRegex.IsMatch(text))
            {
                retVal = true;
            }

            return retVal;

        }
    }
}