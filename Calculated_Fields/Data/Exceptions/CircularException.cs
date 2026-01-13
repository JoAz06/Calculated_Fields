namespace Calculated_Fields.Data.Exceptions {
    public class CircularException : Exception {
        public CircularException()
        : base("Circular reference occured.") {
        }

        public CircularException(string message)
            : base(message) {
        }
    }
}
