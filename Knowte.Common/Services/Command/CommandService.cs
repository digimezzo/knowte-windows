using Knowte.Common.Prism;
using Microsoft.Practices.ServiceLocation;
using Prism.Events;
using System;

namespace Knowte.Common.Services.Command
{
    public class CommandService : ICommandService
    {
        #region Public
        public void NewNote()
        {
            ApplicationCommands.NewNoteCommand.Execute(true);
        }

        public void OpenNote(string noteTitle)
        {
            IEventAggregator eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregator.GetEvent<OpenNoteEvent>().Publish(noteTitle);
        }

        public void Show()
        {
            IEventAggregator eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregator.GetEvent<ShowMainWindowEvent>().Publish(String.Empty);
        }
        #endregion
    }
}
