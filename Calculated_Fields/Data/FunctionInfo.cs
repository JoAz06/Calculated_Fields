namespace Calculated_Fields.Data {
    public class FunctionInfo {
        public string name { get; set; }
        public string equation { get; set; }
        public string tooltip { get; set; }

        public FunctionInfo(string name, string equation, string tooltip) {
            this.name = name;
            this.equation = equation;
            this.tooltip = tooltip;
        }
    }
}
