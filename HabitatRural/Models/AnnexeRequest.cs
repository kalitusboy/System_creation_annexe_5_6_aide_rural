using System;

namespace HabitatRural.Models;

/// <summary>
/// نوع الشطر المطلوب
/// </summary>
public enum TrancheType { T1Only, T2Only, Both }

/// <summary>
/// نوع البناء
/// </summary>
public enum BuildingType { Talia, Ardhi }

/// <summary>
/// نوع الملحق
/// </summary>
public enum AnnexeType { Annexe05, Annexe06 }

/// <summary>
/// طلب توليد ملحق (يحوي جميع المدخلات المطلوبة لحقن قالب PDF).
/// </summary>
public class AnnexeRequest
{
    public Beneficiaire Beneficiaire { get; set; } = new();
    public AnnexeType   AnnexeType   { get; set; } = AnnexeType.Annexe05;
    public TrancheType  Tranche      { get; set; } = TrancheType.T1Only;
    public BuildingType Building     { get; set; } = BuildingType.Talia;

    // التواريخ الثلاثة الاختيارية التي يُسمح بتركها فارغة
    public DateTime? DateDemande { get; set; }       // ملحق 05
    public DateTime? DateVisite  { get; set; }       // ملحق 06
    public DateTime? DatePV      { get; set; }       // ملحق 06

    // خاصة بملحق 06
    public string   Subdivisionnaire { get; set; } = "";
    public string   NumeroPermis     { get; set; } = "";
    public DateTime DatePermis       { get; set; } = DateTime.Today;
    public string   Observation1     { get; set; } = "";
    public string   Observation2     { get; set; } = "";
    public string   ObservationComp  { get; set; } = "";
}
