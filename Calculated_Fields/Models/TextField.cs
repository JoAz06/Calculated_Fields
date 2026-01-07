namespace Calculated_Fields.Models
{
    public enum Type
    {
        NUMERICAL,
        CALCULATED
    }
    public class TextField
    {
        public int Id { get; set; }
        public string name { get; set; }
        public Type type { get; set; }
        public string value { get; set; }

    }
}
