namespace HabitatRural.Models;

/// <summary>
/// نتيجة حساب المنطق المالي والنصي (تُكافئ recomputeLogic في HTML).
/// </summary>
public class AnnexeLogic
{
    public decimal Amount       { get; set; }
    public string  AmountNum    { get; set; } = "";   // 420.000.00
    public string  AmountTxt    { get; set; } = "";   // QUATRE CENT VINGT MILLE DINARS (EN LETTRES)

    public string Pct1Num { get; set; } = "100 %";
    public string Pct1Txt { get; set; } = "CENT POUR CENT";
    public string Pct2Num { get; set; } = "100 %";
    public string Pct2Txt { get; set; } = "CENT POUR CENT";

    public string Rubrique1 { get; set; } = "";
    public string Rubrique2 { get; set; } = "";

    public bool IsT1 { get; set; } = true;
    public bool IsT2 { get; set; } = true;
}
