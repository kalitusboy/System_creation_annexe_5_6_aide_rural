using System;
using System.Collections.Generic;
using System.Linq;
using HabitatRural.Models;

namespace HabitatRural.Services;

public static class BeneficiairesRepository
{
    public static IReadOnlyList<Beneficiaire> All => DatabaseService.Instance.GetAllBeneficiaires();
    public static IEnumerable<Beneficiaire> Search(string? q) => DatabaseService.Instance.SearchBeneficiaires(q);
    public static Beneficiaire? Get(int id) => DatabaseService.Instance.GetBeneficiaireById(id);
    public static void Save(Beneficiaire b) => DatabaseService.Instance.SaveBeneficiaire(b);
    public static void Delete(int id) => DatabaseService.Instance.DeleteBeneficiaire(id);
}
