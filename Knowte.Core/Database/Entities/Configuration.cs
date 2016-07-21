using SQLite.Net.Attributes;

namespace Knowte.Core.Database.Entities
{
    public class Configuration
    {
        #region Properties
        [PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        #endregion
    }
}
