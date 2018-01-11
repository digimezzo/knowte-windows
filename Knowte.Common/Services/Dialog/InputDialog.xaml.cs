using Digimezzo.WPFControls;
using Knowte.Common.Base;
using Knowte.Common.Extensions;
using System.Windows;

namespace Knowte.Common.Services.Dialog
{
    public partial class InputDialog : BorderlessWindows8Window
    {
        private string responseText;
    
        public string ResponseText
        {
            get { return this.responseText; }
            set { this.responseText = value; }
        }
    
        public InputDialog(Window parent, string title, string content, string okText, string cancelText, string defaultResponse) : base()
        {
            InitializeComponent();

            this.TitleBarHeight = Defaults.DefaultWindowButtonHeight + 10;

            this.Title = title;
            this.TextBlockTitle.Text = title;
            this.TextBlockContent.Text = content;
            this.ButtonOK.Content = okText;
            this.ButtonCancel.Content = cancelText;
            this.TextBoxResponse.Text = defaultResponse;

            this.CenterWindow(parent);
        }
    
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.ResponseText = TextBoxResponse.Text;
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}