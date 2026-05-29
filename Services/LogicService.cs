using HabitatRural.Models;

namespace HabitatRural.Services;

public static class LogicService
{
    public static AnnexeLogic Compute(AppSettings settings, AnnexeRequest request)
    {
        var logic = new AnnexeLogic();
        var total = settings.TotalAmount > 0 ? settings.TotalAmount : 700000m;
        var pct1 = settings.PctT1;
        var pct2 = settings.PctT2;
        var amt1 = decimal.Round(total * pct1 / 100m);
        var amt2 = decimal.Round(total * pct2 / 100m);
        decimal amount;
        switch (request.Tranche)
        {
            case TrancheType.T1Only:
                amount = amt1;
                logic.Pct1Num = "100 %"; logic.Pct1Txt = "CENT POUR CENT";
                logic.Pct2Num = "0 %";   logic.Pct2Txt = "ZERO POUR CENT";
                break;
            case TrancheType.T2Only:
                amount = amt2;
                logic.Pct1Num = "100 %"; logic.Pct1Txt = "CENT POUR CENT";
                logic.Pct2Num = "100 %"; logic.Pct2Txt = "CENT POUR CENT";
                break;
            default:
                amount = total;
                logic.Pct1Num = "100 %"; logic.Pct1Txt = "CENT POUR CENT";
                logic.Pct2Num = "100 %"; logic.Pct2Txt = "CENT POUR CENT";
                break;
        }
        logic.Amount = amount;
        logic.AmountNum = NumberToFrenchWords.FormatAmount(amount);
        logic.AmountTxt = NumberToFrenchWords.Convert((long)amount) + " DINARS (EN LETTRES)";
        if (request.Building == BuildingType.Talia)
        { logic.Rubrique1 = "ACHEVEMENT DES POTEAUX"; logic.Rubrique2 = "ACHEVEMENT DE PLANCHER"; }
        else
        { logic.Rubrique1 = "ACHEVEMENT DE PLATE-FORME"; logic.Rubrique2 = "ACHEVEMENT DES POTEAUX"; }
        logic.IsT1 = request.Tranche != TrancheType.T2Only;
        logic.IsT2 = request.Tranche != TrancheType.T1Only;
        return logic;
    }
}
