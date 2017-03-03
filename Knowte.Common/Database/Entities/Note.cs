using SQLite;
using System;

namespace Knowte.Common.Database.Entities
{
    public class Note
    {
        #region Properties
        [PrimaryKey()]
        public string Id { get; set; }
        public string NotebookId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public long CreationDate { get; set; }
        public long OpenDate { get; set; }
        public long ModificationDate { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public long Top { get; set; }
        public long Left { get; set; }
        public long Maximized { get; set; }
        public long Flagged { get; set; }
        #endregion

        #region Overrides
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

            return this.Title.Equals(((Note)obj).Title);
        }

        public override int GetHashCode()
        {
            return this.Title.GetHashCode();
        }
        #endregion
    }
}
