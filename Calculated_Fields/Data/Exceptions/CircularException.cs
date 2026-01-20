namespace Calculated_Fields.Data.Exceptions {
    public class CircularException : Exception {
        public CircularException()
        : base("#CIRCULAR!") {
        }

        public CircularException(string message)
            : base(message) {
        }
    }
}
