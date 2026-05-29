using System;
using System.Globalization;
using System.Text;

namespace HabitatRural.Services;

public static class NumberToFrenchWords
{
    private static readonly string[] Units = { "", "UN", "DEUX", "TROIS", "QUATRE", "CINQ", "SIX", "SEPT", "HUIT", "NEUF", "DIX", "ONZE", "DOUZE", "TREIZE", "QUATORZE", "QUINZE", "SEIZE", "DIX-SEPT", "DIX-HUIT", "DIX-NEUF" };
    private static readonly string[] Tens = { "", "", "VINGT", "TRENTE", "QUARANTE", "CINQUANTE", "SOIXANTE", "SOIXANTE", "QUATRE-VINGT", "QUATRE-VINGT" };

    public static string Convert(long num)
    {
        if (num == 0) return "ZERO";
        var sb = new StringBuilder();
        long million = num / 1_000_000;
        long thousand = (num % 1_000_000) / 1_000;
        long rest = num % 1_000;
        if (million > 0) sb.Append(million == 1 ? "UN MILLION" : Below1000(million) + " MILLIONS").Append(' ');
        if (thousand > 0) sb.Append(thousand == 1 ? "MILLE" : Below1000(thousand) + " MILLE").Append(' ');
        if (rest > 0) sb.Append(Below1000(rest));
        return sb.ToString().Trim();
    }

    public static string FormatAmount(decimal num)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var s = num.ToString("N2", fr);
        return s.Replace(" ", ".").Replace(",", ".");
    }

    private static string Below1000(long n)
    {
        var sb = new StringBuilder();
        long h = n / 100;
        long r = n % 100;
        if (h > 0)
        {
            if (h == 1) sb.Append("CENT");
            else sb.Append(Units[h]).Append(" CENT").Append(r == 0 && h > 1 ? "S" : "");
        }
        if (r > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            if (r < 20) { sb.Append(Units[r]); return sb.ToString(); }
            long t = r / 10;
            long u = r % 10;
            if (t == 7 || t == 9)
            {
                sb.Append(Tens[t]).Append(u == 1 && t == 7 ? " ET " : "-").Append(Units[10 + u]);
            }
            else
            {
                sb.Append(Tens[t]);
                if (u == 1 && t < 8) sb.Append(" ET UN");
                else if (u > 0) sb.Append('-').Append(Units[u]);
                else if (t == 8) sb.Append('S');
            }
        }
        return sb.ToString();
    }
}
