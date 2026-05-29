using System;

namespace HabitatRural.Models;

public enum TrancheType { T1Only, T2Only, Both }
public enum BuildingType { Talia, Ardhi }
public enum AnnexeType { Annexe05, Annexe06 }

public class AnnexeRequest
{
    public Beneficiaire Beneficiaire { get; set; } = new();
    public AnnexeType   AnnexeType   { get; set; } = AnnexeType.Annexe05;
    public TrancheType  Tranche      { get; set; } = TrancheType.T1Only;
    public BuildingType Building     { get; set; } = BuildingType.Talia;
    public DateTime? DateDemande { get; set; }
    public DateTime? DateVisite  { get; set; }
    public DateTime? DatePV      { get; set; }
    public string   Subdivisionnaire { get; set; } = "";
    public string   NumeroPermis     { get; set; } = "";
    public DateTime DatePermis       { get; set; } = DateTime.Today;
    public string   Observation1     { get; set; } = "";
    public string   Observation2     { get; set; } = "";
    public string   ObservationComp  { get; set; } = "";
}
