namespace Calculated_Fields.Data.Exceptions {
    public class MissingParameterException : Exception {
        public MissingParameterException()
        : base("Variable does not exist.") {
        }

        public MissingParameterException(string message)
            : base(message) {
        }
    }
}
