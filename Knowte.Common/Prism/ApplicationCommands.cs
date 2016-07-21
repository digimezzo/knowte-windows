using Prism.Commands;

namespace Knowte.Common.Prism
{
    public class ApplicationCommands
    {
        public static CompositeCommand NavigateBetweenMainCommand = new CompositeCommand();
        public static CompositeCommand NavigateBetweenNotesCommand = new CompositeCommand();
        public static CompositeCommand NavigateBetweenSettingsCommand = new CompositeCommand();
        public static CompositeCommand NavigateBetweenInformationCommand = new CompositeCommand();
        public static CompositeCommand OpenLinkCommand = new CompositeCommand();
        public static CompositeCommand OpenPathCommand = new CompositeCommand();
        public static CompositeCommand NewNotebookCommand = new CompositeCommand();
        public static CompositeCommand NewNoteCommand = new CompositeCommand();
        public static CompositeCommand ImportNoteCommand = new CompositeCommand();
    }
}