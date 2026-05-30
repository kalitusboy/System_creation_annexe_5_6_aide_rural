using System.Collections.Generic;
namespace HabitatRural.Models {
    public class AppSettings {
        public string Wilaya { get; set; } = "CHLEF";
        public string Daira { get; set; } = "CHLEF";
        public string Commune { get; set; } = "Deux bassins";
        public decimal TotalAmount { get; set; } = 700000m;
        public int PctT1 { get; set; } = 60;
        public int PctT2 { get; set; } = 40;
        public List<string> Subdivisionnaires { get; set; } = new();
        public bool FirstRun { get; set; } = true;
    }
}