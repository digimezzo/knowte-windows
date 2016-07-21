namespace Knowte.Core.Helpers
{
    public class FontSizeCorrection
    {

        #region Variables
        private string name;
        private int correction;
        #endregion

        #region Properties
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
        #endregion

        #region Public
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
        #endregion
    }
}