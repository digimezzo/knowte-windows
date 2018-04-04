using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Helpers;
using Knowte.Common.Services.Note;
using Knowte.Common.Utils;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace Knowte.SettingsModule.ViewModels
{
    public class SettingsNotesViewModel : BindableBase
    {
        private INoteService noteService;
        private ObservableCollection<FontSizeCorrection> fontSizeCorrections;
        private FontSizeCorrection selectedFontSizeCorrection;
        private int previewFontSize;
        private bool checkBoxEscapeChecked;
        private bool checkBoxUseExactDatesChecked;

        public ObservableCollection<FontSizeCorrection> FontSizeCorrections
        {
            get { return this.fontSizeCorrections; }
            set { SetProperty<ObservableCollection<FontSizeCorrection>>(ref this.fontSizeCorrections, value); }
        }

        public FontSizeCorrection SelectedFontSizeCorrection
        {
            get { return this.selectedFontSizeCorrection; }
            set
            {
                SettingsClient.Set<int>("Notes", "FontSizeCorrection", value.Correction);
                PreviewFontSize = Defaults.DefaultNoteFontSize + value.Correction;
                SetProperty<FontSizeCorrection>(ref this.selectedFontSizeCorrection, value);
            }
        }

        public int PreviewFontSize
        {
            get { return this.previewFontSize; }
            set { SetProperty<int>(ref this.previewFontSize, value); }
        }

        public bool CheckBoxEscapeChecked
        {
            get { return this.checkBoxEscapeChecked; }
            set
            {
                SettingsClient.Set<bool>("Notes", "PressingEscapeClosesNotes", value);
                SetProperty<bool>(ref this.checkBoxEscapeChecked, value);
            }
        }

        public bool CheckBoxUseExactDatesChecked
        {
            get { return this.checkBoxUseExactDatesChecked; }
            set
            {
                SettingsClient.Set<bool>("Notes", "UseExactDates", value);
                SetProperty<bool>(ref this.checkBoxUseExactDatesChecked, value);
                this.noteService.OnNotesChanged();
            }
        }

        public SettingsNotesViewModel(INoteService noteService)
        {
            this.noteService = noteService;
            this.LoadFontSizeCorrections();
            this.LoadCheckBoxStates();
        }
    
        private void LoadCheckBoxStates()
        {
            this.CheckBoxEscapeChecked = SettingsClient.Get<bool>("Notes", "PressingEscapeClosesNotes");
            this.checkBoxUseExactDatesChecked = SettingsClient.Get<bool>("Notes", "UseExactDates");
        }

        private void LoadFontSizeCorrections()
        {
            this.FontSizeCorrections = new ObservableCollection<FontSizeCorrection>();
            this.FontSizeCorrections.Add(new FontSizeCorrection
            {
                Name = ResourceUtils.GetString("Language_Normal"),
                Correction = 0
            });
            this.FontSizeCorrections.Add(new FontSizeCorrection
            {
                Name = ResourceUtils.GetString("Language_Large"),
                Correction =3
            });
            this.FontSizeCorrections.Add(new FontSizeCorrection
            {
                Name = ResourceUtils.GetString("Language_Larger"),
                Correction = 6
            });

            foreach (FontSizeCorrection fzc in this.FontSizeCorrections)
            {
                if (SettingsClient.Get<int>("Notes", "FontSizeCorrection") == fzc.Correction)
                {
                    this.SelectedFontSizeCorrection = fzc;
                }
            }
        }
    }
}