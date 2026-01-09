using System.ComponentModel.DataAnnotations;
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
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string name { get; set; }
        [Required]
        public Type type { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
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
