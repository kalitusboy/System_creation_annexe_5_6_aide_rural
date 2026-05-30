using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HabitatRural.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.IO;

namespace HabitatRural.Services {
    public class PdfFiller {
        public List<string> Logs { get; } = new();
        public void Log(string msg, bool error = false) => Logs.Add((error ? "❌ " : "✅ ") + msg);
        public void Clear() => Logs.Clear();
        public byte[] FillAnnexe05(string templatePath, AppSettings settings, AnnexeRequest req, AnnexeLogic logic) {
            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
            var form = doc.AcroForm;
            if (form == null) throw new InvalidOperationException("القالب لا يحتوي على حقول AcroForm.");
            EnsureNeedAppearances(doc);
            var b = req.Beneficiaire;
            SetText(form, "Code_beneficiare", b.Code);
            SetText(form, "nom_prenom_prenom_pere_benificiare", b.NomPrenom);
            SetText(form, "adresse_beneficiare", b.Adresse);
            SetText(form, "Dernier_cinq_chiffre_code_beneficiare", b.Last5);
            SetText(form, "date_decision", b.DateDecision.ToString("dd/MM/yyyy"));
            SetText(form, "Numero_ccp", b.NumeroCcp);
            SetText(form, "date_demande", req.DateDemande?.ToString("dd/MM/yyyy") ?? "");
            SetAllByName(form, "Commune", settings.Commune);
            SetText(form, "Montant_en_chiffre", logic.AmountNum);
            SetText(form, "MONTANT_EN_LETTRE", logic.AmountTxt);
            SetCheckBox(form, "Premiere_TRANCHE", logic.IsT1);
            SetCheckBox(form, "Deuxième_TRANCHE", logic.IsT2);
            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }
        public byte[] FillAnnexe06(string templatePath, AppSettings settings, AnnexeRequest req, AnnexeLogic logic) {
            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
            var form = doc.AcroForm;
            if (form == null) throw new InvalidOperationException("القالب لا يحتوي على حقول AcroForm.");
            EnsureNeedAppearances(doc);
            var b = req.Beneficiaire;
            SetText(form, "Wilaya", settings.Wilaya);
            SetAllByName(form, "Daira", settings.Daira);
            SetAllByName(form, "Commune", settings.Commune);
            SetText(form, "Nom_subdivisionnaire", req.Subdivisionnaire);
            SetText(form, "nom_prenom_prenom_pere_benificiare", b.NomPrenom);
            SetText(form, "adresse_beneficiare", b.Adresse);
            SetText(form, "Dernier_cinq_chiffre_code_beneficiare", b.Last5);
            SetText(form, "date_decision", b.DateDecision.ToString("dd/MM/yyyy"));
            SetText(form, "Date_visite", req.DateVisite?.ToString("dd/MM/yyyy") ?? "");
            SetText(form, "date_elaboration_de_pv", req.DatePV?.ToString("dd/MM/yyyy") ?? "");
            SetText(form, "Numero_permis_de_construire", req.NumeroPermis);
            SetText(form, "Date_permis_de_construire", req.DatePermis.ToString("dd/MM/yyyy"));
            SetText(form, "Type_de_travaux_1_tranche", logic.Rubrique1);
            SetText(form, "Type_de_travaux_2_tranche", logic.Rubrique2);
            SetText(form, "Pourcentage_de_travaux_1_tranche_en_chiffre", logic.Pct1Num);
            SetText(form, "Pourcentage_de_travaux_2_tranche_en_chiffre", logic.Pct2Num);
            SetText(form, "Pourcentage_de_travaux_1_tranche_en_lettre", logic.Pct1Txt);
            SetText(form, "Pourcentage_de_travaux_2_tranche_en_lettre", logic.Pct2Txt);
            SetText(form, "Observation_1_tranche", req.Observation1);
            SetText(form, "Observation_2_tranche", req.Observation2);
            SetText(form, "Observation_complimentaire", req.ObservationComp);
            SetCheckBox(form, "Premiere_tranche", logic.IsT1);
            SetCheckBox(form, "Deuxième_tranche", logic.IsT2);
            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }
        private static void EnsureNeedAppearances(PdfDocument doc) {
            if (doc.AcroForm.Elements.ContainsKey("/NeedAppearances"))
                doc.AcroForm.Elements["/NeedAppearances"] = new PdfBoolean(true);
            else
                doc.AcroForm.Elements.Add("/NeedAppearances", new PdfBoolean(true));
        }
        private void SetText(PdfAcroForm form, string name, string value) {
            var field = form.Fields[name];
            if (field == null) { Log($"حقل غير موجود: {name}", true); return; }
            try {
                if (field is PdfTextField tf) tf.Value = new PdfString(value);
                else field.Elements.SetString("/V", value);
                Log($"{name} = {value}");
            } catch (Exception ex) { Log($"{name}: {ex.Message}", true); }
        }
        private void SetAllByName(PdfAcroForm form, string name, string value) {
            int count = 0;
            for (int i = 0; i < form.Fields.Count; i++) {
                var f = form.Fields[i];
                if (f != null && f.Name == name) {
                    try {
                        if (f is PdfTextField tf) tf.Value = new PdfString(value);
                        else f.Elements.SetString("/V", value);
                        count++;
                    } catch { }
                }
            }
            if (count > 0) Log($"{count}× \"{name}\" = {value}");
            else Log($"حقل غير موجود: {name}", true);
        }
        private void SetCheckBox(PdfAcroForm form, string name, bool isChecked) {
            var field = form.Fields[name];
            if (field == null) { Log($"حقل غير موجود: {name}", true); return; }
            try {
                if (field is PdfCheckBoxField cb) cb.Checked = isChecked;
                else field.Elements.SetName("/V", isChecked ? "/Yes" : "/Off");
                Log($"☑ {name} = {isChecked}");
            } catch (Exception ex) { Log($"{name}: {ex.Message}", true); }
        }
        public static List<string> ListFields(string templatePath) {
            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.ReadOnly);
            var form = doc.AcroForm;
            if (form == null) return new List<string>();
            var names = new List<string>();
            for (int i = 0; i < form.Fields.Count; i++) {
                var f = form.Fields[i];
                if (f != null) names.Add(f.Name);
            }
            return names.Distinct().ToList();
        }
    }
}