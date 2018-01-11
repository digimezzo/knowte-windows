namespace Knowte.Common.Helpers
{
    public class FontSizeCorrection
    {
        private string name;
        private int correction;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public int Correction
        {
            get { return this.correction; }
            set { this.correction = value; }
        }
     
        public override string ToString()
        {
            return this.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Correction.Equals(((FontSizeCorrection)obj).Correction);
        }

        public override int GetHashCode()
        {
            return this.Correction.GetHashCode();
        }
    }
}