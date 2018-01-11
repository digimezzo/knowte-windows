using SQLite;
using System;

namespace Knowte.Common.Database.Entities
{
    public class Notebook
    {
        [PrimaryKey()]
        public string Id { get; set; }
        public string Title { get; set; }
        public long CreationDate { get; set; }

        [Ignore()]
        public bool IsDefaultNotebook { get; set; }
     
        public override string ToString()
        {
            return this.Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Id.Equals(((Notebook)obj).Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
