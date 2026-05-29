# 📋 نظام ملاحق المساعدة الريفية (C# WPF)

> برنامج Windows سطح المكتب لتعبئة الملحقين **Annexe 05** (طلب دفع الإعانة) و**Annexe 06** (محضر معاينة الأشغال) آلياً، عبر **حقن البيانات مباشرة داخل القوالب الرسمية للـ PDF** للحصول على ملفات بالتنسيق الرسمي تماماً.

## ✨ الميزات الرئيسية

- 🎨 **واجهة عصرية وأنيقة** مع تدرجات لونية، حقول مدوّرة، أزرار "حبوب" RTL مع دعم كامل للغة العربية.
- 🧠 **نفس منطق ملف HTML تماماً** (Talia/Ardhi، الشطر 1/2/كلاهما، نِسب وحروف ديناميكية).
- 📥 **حقن مباشر داخل قوالب PDF الرسمية** المضمّنة في البرنامج (`Annexe_05_Officiel.pdf` و `Annexe_06_Officiel.pdf`) → المخرج بالتنسيق الرسمي 100%.
- ⚙️ **نافذة إعدادات عامة منفصلة**:
  - الولاية / الدائرة / البلدية
  - **المبلغ الإجمالي للإعانة قابل للتعديل** (افتراضي 700,000 دج)
  - **نِسب الأشطر قابلة للتعديل** (افتراضي 60% / 40%)
  - **قائمة Subdivisionnaires المحتملين** قابلة للتحرير
  - الإعدادات تُحفظ في JSON تحت `%LocalAppData%\HabitatRural\settings.json`
  - أول تشغيل يفتح النافذة تلقائياً
- 📅 **3 تواريخ فقط يُسمح بتركها فارغة** (للكتابة اليدوية على المطبوع):
  - تاريخ الطلب (Date demande) — ملحق 05
  - تاريخ المعاينة (Date visite) — ملحق 06
  - تاريخ إعداد المحضر (Date PV) — ملحق 06
- 💾 **قاعدة بيانات للمستفيدين** (JSON محلي) — تحميل/حفظ/حذف بسهولة.
- 🔧 **سجل تشخيص (Debug)** لعرض حقول القالب والعمليات.

## 🏗 البنية

```
HabitatRural/
├── Models/
│   ├── AppSettings.cs          ← الإعدادات العامة
│   ├── Beneficiaire.cs         ← المستفيد
│   ├── AnnexeRequest.cs        ← طلب توليد ملحق
│   └── AnnexeLogic.cs          ← نتيجة الحساب
├── Services/
│   ├── SettingsService.cs      ← تحميل/حفظ الإعدادات (JSON)
│   ├── BeneficiairesRepository.cs ← مستودع المستفيدين (JSON)
│   ├── NumberToFrenchWords.cs  ← تحويل الأرقام إلى كلمات فرنسية
│   ├── LogicService.cs         ← منطق الحساب (مكافئ recomputeLogic)
│   └── PdfFiller.cs            ← حقن البيانات في PDF (PdfSharpCore)
├── Views/
│   ├── MainWindow.xaml(.cs)    ← الواجهة الرئيسية
│   ├── SettingsWindow.xaml(.cs)← نافذة الإعدادات
│   └── AboutWindow.xaml(.cs)   ← نافذة "حول"
├── Resources/
│   ├── Theme.xaml              ← الألوان والـ Brushes
│   └── Controls.xaml           ← أنماط TextBox/Button/Pill...
├── Assets/Templates/
│   ├── Annexe_05_Officiel.pdf  ← القالب الرسمي
│   └── Annexe_06_Officiel.pdf
├── App.xaml(.cs)
├── HabitatRural.csproj
└── app.manifest
```

## 🚀 البناء والتشغيل

### المتطلبات
- **Windows 10/11**
- **.NET 8 SDK** ([تحميل](https://dotnet.microsoft.com/download/dotnet/8.0))

### البناء
```bash
cd HabitatRural
dotnet restore
dotnet build -c Release
```

### التشغيل
```bash
dotnet run --project HabitatRural -c Release
```

### إنشاء ملف تنفيذي مستقل (self-contained)
```bash
dotnet publish HabitatRural -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

سيتم إنشاء ملف `HabitatRural.exe` واحد في `bin\Release\net8.0-windows\win-x64\publish\` يمكن تشغيله على أي ويندوز بدون تثبيت .NET.

## 📦 الحزم المستخدمة

- **PdfSharpCore 1.3.65** — حقن البيانات في قوالب PDF (AcroForm).
- **System.Text.Json** — حفظ الإعدادات والبيانات في JSON (مدمج في .NET 8).

## 🔑 منطق الحساب (مطابق لـ HTML)

### الشطر:
| الاختيار | المبلغ | نسبة 1 | نسبة 2 |
|---|---|---|---|
| الشطر 1 فقط | `Total × Pct1/100` | 100% (CENT POUR CENT) | 0% (ZERO POUR CENT) |
| الشطر 2 فقط | `Total × Pct2/100` | 100% | 100% |
| الشطرين معاً | `Total` | 100% | 100% |

### نوع البناء:
| النوع | Rubrique 1 | Rubrique 2 |
|---|---|---|
| تعلية (Talia) | ACHEVEMENT DES POTEAUX | ACHEVEMENT DE PLANCHER |
| أرضية (Ardhi) | ACHEVEMENT DE PLATE-FORME | ACHEVEMENT DES POTEAUX |

## ✍ المطوّر

**حميتي نسيم الحوضان**
