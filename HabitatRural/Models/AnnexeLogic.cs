namespace HabitatRural.Models {
    public class AnnexeLogic {
        public decimal Amount { get; set; }
        public string AmountNum { get; set; } = "";
        public string AmountTxt { get; set; } = "";
        public string Pct1Num { get; set; } = "100 %";
        public string Pct1Txt { get; set; } = "CENT POUR CENT";
        public string Pct2Num { get; set; } = "100 %";
        public string Pct2Txt { get; set; } = "CENT POUR CENT";
        public string Rubrique1 { get; set; } = "";
        public string Rubrique2 { get; set; } = "";
        public bool IsT1 { get; set; } = true;
        public bool IsT2 { get; set; } = true;
    }
}