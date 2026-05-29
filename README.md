# 📋 نظام ملاحق المساعدة الريفية (HabitatRural)

> **C# WPF** application to automatically fill **Annexes 05 & 06** of the Algerian rural housing aid, by injecting data directly into official PDF templates.

---

## ✨ الميزات (Features)

- ✅ **تعبئة آلية** للملحق 05 (طلب دفع الإعانة) والملحق 06 (محضر معاينة الأشغال).
- 🎨 **واجهة عصرية** بتصميم RTL، أزرار "حبوب"، ألوان متدرجة، ودعم كامل للغة العربية.
- 🧠 **منطق ديناميكي** (Talia/Ardhi، الشطر الأول/الثاني/كلاهما، نسب قابلة للتعديل).
- 📥 **حقن مباشر في قوالب PDF الرسمية** باستخدام `PdfSharpCore` → مخرجات مطابقة للنسخة الرسمية 100%.
- ⚙️ **نافذة إعدادات عامة**:
  - الولاية / الدائرة / البلدية.
  - المبلغ الإجمالي للإعانة (قابل للتعديل).
  - نسب الشطر الأول والثاني (مجموعها 100%).
  - قائمة `Subdivisionnaires` قابلة للتحرير.
- 💾 **قاعدة بيانات SQLite** محلية (جدولان: `Beneficiaires` و `Archives`).
- 🔍 **البحث بآخر 5 أرقام** من `Code bénéficiaire` أو بالاسم.
- 📁 **حفظ نسخة أرشيفية** من كل PDF مولَّد في مجلد `Documents/HabitatRural_Archives`، باسم يحتوي على: نوع الملحق، الكود، الاسم، نوع الشطر (T1/T2/Both)، والتاريخ.
- 📅 **3 تواريخ اختيارية** يمكن تركها فارغة للكتابة اليدوية (تاريخ الطلب، تاريخ المعاينة، تاريخ إعداد المحضر).
- 🔧 **سجل تشخيص (Debug)** لعرض أسماء حقول القالب وأخطاء التعبئة.
- 🚀 **أول تشغيل** يفتح نافذة الإعدادات تلقائياً.

---

## 📦 المتطلبات (Requirements)

- **Windows 10 / 11** (64-bit)
- **.NET 8 SDK** (للبناء من المصدر) – [تحميل](https://dotnet.microsoft.com/download/dotnet/8.0)
- أو تشغيل الملف التنفيذي المستقل (self-contained) بدون تثبيت .NET.

---

## 🛠️ البناء من المصدر (Build from source)

```bash
git clone https://github.com/yourusername/HabitatRural.git
cd HabitatRural
dotnet restore
dotnet build -c Release