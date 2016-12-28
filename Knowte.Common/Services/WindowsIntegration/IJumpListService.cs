using Knowte.Common.Base;
using Knowte.Common.Database.Entities;
using System.Collections.Generic;

namespace Knowte.Common.Services.WindowsIntegration
{
    public interface IJumpListService
    {
        string OpenNoteFromJumplistTitle { get; set; }
        bool OpenNoteFromJumplist { get; set; }
        bool NewNoteFromJumplist { get; set; }
        void RefreshJumpListAsync(List<Note> recentNotes, List<Note> flaggedNotes);
    }
}