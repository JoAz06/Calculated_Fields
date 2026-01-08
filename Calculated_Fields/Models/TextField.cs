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

        public TextField() { }

        public TextField(string name, string value, bool type) {
            this.name = name;
            this.value = value;
            if (type) {
                this.type = Type.CALCULATED;
            } else {
                this.type = Type.NUMERICAL;
            }
        }

    }
}
