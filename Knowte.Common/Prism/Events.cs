using Prism.Events;

namespace Knowte.Common.Prism
{
    public class RefreshJumpListEvent : PubSubEvent<string>
    {
    }

    public class OpenNoteEvent : PubSubEvent<string>
    {
    }

    public class NotebooksChangedEvent : PubSubEvent<string>
    {
    }

    public class RefreshNotebooksEvent : PubSubEvent<string>
    {
    }

    public class CountNotesEvent : PubSubEvent<string>
    {
    }

    public class SetMainSearchBoxFocusEvent : PubSubEvent<string>
    {
    }

    public class ShowMainWindowEvent : PubSubEvent<string>
    {
    }

    public class TriggerLoadNoteAnimationEvent : PubSubEvent<string>
    {
    }

    public class SettingChangeStorageLocationFromMainChangedEvent : PubSubEvent<string>
    {
    }
}