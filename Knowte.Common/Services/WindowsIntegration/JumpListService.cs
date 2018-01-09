using Knowte.Common.Base;
using Knowte.Common.Database.Entities;
using Knowte.Common.Utils;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace Knowte.Common.Services.WindowsIntegration
{
    public class JumpListService : IJumpListService
    {
        #region Variables
        private JumpList jumplist;
        #endregion

        #region Constructor
        public JumpListService()
        {
            this.jumplist = JumpList.GetJumpList(Application.Current);
        }
        #endregion

        #region Properties
        public bool OpenNoteFromJumplist { get; set; }
        public string OpenNoteFromJumplistTitle { get; set; }
        public bool NewNoteFromJumplist { get; set; }
        #endregion

        #region Private
        private List<JumpTask> CreateRecentNotesJumpTasks(List<Database.Entities.Note> recentNotes)
        {
            List<JumpTask> jtList = new List<JumpTask>();

            foreach (Database.Entities.Note note in recentNotes)
            {
                jtList.Add(new JumpTask
                {
                    Title = note.Title,
                    Arguments = "/open " + "\"" + note.Title + "\"",
                    Description = "",
                    CustomCategory = ResourceUtils.GetString("Language_Recent_Notes"),
                    IconResourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Defaults.IconsLibrary),
                    ApplicationPath = Assembly.GetEntryAssembly().Location,
                    IconResourceIndex = 2
                });
            }

            return jtList;
        }
        #endregion

        #region Public
        public async void RefreshJumpListAsync(List<Database.Entities.Note> recentNotes, List<Database.Entities.Note> flaggedNotes)
        {
            await Task.Run(() =>
            {
                if (this.jumplist != null)
                {
                    this.jumplist.JumpItems.Clear();
                    this.jumplist.ShowFrequentCategory = false;
                    this.jumplist.ShowRecentCategory = false;

                    foreach (JumpTask task in this.CreateRecentNotesJumpTasks(recentNotes))
                    {
                        this.jumplist.JumpItems.Add(task);
                    }

                    this.jumplist.JumpItems.Add(new JumpTask
                    {
                        Title = ResourceUtils.GetString("Language_Donate"),
                        Arguments = "/donate " + ProductInformation.PayPalLink,
                        Description = "",
                        IconResourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Defaults.IconsLibrary),
                        ApplicationPath = Assembly.GetEntryAssembly().Location,
                        IconResourceIndex = 0
                    });

                    this.jumplist.JumpItems.Add(new JumpTask
                    {
                        Title = ResourceUtils.GetString("Language_New_Note"),
                        Arguments = "/new dummyArgument",
                        Description = "",
                        IconResourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Defaults.IconsLibrary),
                        ApplicationPath = Assembly.GetEntryAssembly().Location,
                        IconResourceIndex = 1
                    });
                }
            });

            if (this.jumplist != null)
            {
                this.jumplist.Apply();
            }
        }
        #endregion
    }
}
