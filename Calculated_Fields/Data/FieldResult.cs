using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Calculated_Fields.Data {
    public class FieldResult {
        public bool valid { get; set; }
        public string result { get; set; }

        public FieldResult(bool valid, string result) {
            this.valid = valid;
            this.result = result;
        }
   
        public override string ToString() {
            return result;
        }
    }
}
