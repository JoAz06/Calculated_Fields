using System.ComponentModel.DataAnnotations;
namespace Calculated_Fields.Models
{
    public enum Type
    {
        NUMERICAL,
        CALCULATED
    }
    public class TextField : IEquatable<TextField> {
        public int Id { get; set; }
        [Required]
        [MinLength(1)]
        public string name { get; set; }
        [Required]
        public Type type { get; set; }
        [Required]
        [MinLength(1)]
        public string value { get; set; }

        public TextField() { }

        public TextField(string name, string value = "0", bool type = false) {
            this.name = name;
            if (string.IsNullOrWhiteSpace(value))
                this.value = 0.ToString();
            else this.value = value;
            if (type) {
                this.type = Type.CALCULATED;
            } else {
                this.type = Type.NUMERICAL;
            }
        }
        public bool Equals(TextField? other) {
            return other is not null && name.Equals(other.name);
        }

        public override bool Equals(object? obj) {
            return Equals(obj as TextField);
        }

        public override int GetHashCode() {
            return name?.GetHashCode() ?? 0;
        }
    }
}
