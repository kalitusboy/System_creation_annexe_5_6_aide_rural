using System.Collections.Generic;

namespace HabitatRural.Models;

/// <summary>
/// الإعدادات العامة للبرنامج (تُحفظ مرة واحدة وتُستخدم في كل الملفات)
/// تُحفظ في ملف JSON محلي.
/// </summary>
public class AppSettings
{
    // معلومات الجهة الإدارية
    public string Wilaya  { get; set; } = "CHLEF";
    public string Daira   { get; set; } = "CHLEF";
    public string Commune { get; set; } = "Deux bassins";

    // الإعانة المالية القابلة للتعديل
    public decimal TotalAmount { get; set; } = 700000m;
    public int     PctT1       { get; set; } = 60;
    public int     PctT2       { get; set; } = 40;

    // قائمة أسماء Subdivisionnaires المحتملين
    public List<string> Subdivisionnaires { get; set; } = new()
    {
        "AMRANI AHMED",
        "BENALI MOHAMED",
        "KHELIFI YOUCEF"
    };

    // علامة أول تشغيل (true = لم يُكمل المستخدم الإعدادات بعد)
    public bool FirstRun { get; set; } = true;
}
