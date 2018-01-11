namespace Knowte.Common.Services.Appearance
{
    public class ColorScheme
    {
        private string name;
        private string accentColor;
     
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string AccentColor
        {
            get { return this.accentColor; }
            set { this.accentColor = value; }
        }
    
        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Name.Equals(((ColorScheme)obj).Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}