namespace Knowte.Core.Helpers
{
    public class ExternalComponent
    {
        #region Variables
        private string name;
        private string link;
        #endregion

        #region Properties
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Link
        {
            get { return this.link; }
            set { this.link = value; }
        }
        #endregion
    }
}
