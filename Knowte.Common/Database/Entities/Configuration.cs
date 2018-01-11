using SQLite;

namespace Knowte.Common.Database.Entities
{
    public class Configuration
    {
        [PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
