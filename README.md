# 💾 Smart Backup Suite

**نظام إدارة النسخ الاحتياطي المتكامل لقواعد بيانات SQL Server**

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/ahmedzap/SmartBackupSuite)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows-blue.svg)](https://www.microsoft.com/windows)

---

## 📋 نظرة عامة

**Smart Backup Suite** هو تطبيق سطح مكتب متكامل مصمم لإدارة عمليات النسخ الاحتياطي لقواعد بيانات SQL Server بسهولة وكفاءة. يوفر التطبيق واجهة مستخدم عصرية وسهلة الاستخدام، مع دعم متعدد للوجهات السحابية وجدولة مرنة.

### ✨ المميزات الرئيسية

| الميزة | الوصف |
|--------|-------|
| 🗄️ **نسخ احتياطي لـ SQL Server** | دعم Full, Differential, Log Backup |
| ☁️ **وجهات سحابية متعددة** | Google Drive, Dropbox, OneDrive, AWS S3, Azure Blob |
| ⏰ **جدولة مرنة** | يومي، أسبوعي، شهري، فترة زمنية، مخصص |
| 🔐 **أمان متقدم** | تشفير الملفات، ضغط، سياسة احتفاظ |
| 📊 **لوحة تحكم** | إحصائيات ومخططات بيانية متقدمة |
| 📧 **إشعارات البريد الإلكتروني** | إشعارات بنجاح أو فشل العمليات |
| 🖥️ **واجهة عربية** | دعم كامل للغة العربية |

---

## 🚀 متطلبات النظام

| المتطلب | الإصدار |
|---------|---------|
| نظام التشغيل | Windows 10 / 11 |
| .NET Runtime | .NET 10.0 أو أحدث |
| SQL Server | 2012 أو أحدث |
| المساحة | 100 ميجابايت (للتثبيت) |

---

## 📦 المكتبات المستخدمة

```xml
<!-- SQL Server -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.4" />

<!-- Google Drive -->
<PackageReference Include="Google.Apis.Drive.v3" Version="1.68.0.3424" />

<!-- Dropbox -->
<PackageReference Include="Dropbox.Api" Version="7.0.0" />

<!-- OneDrive / Microsoft Graph -->
<PackageReference Include="Microsoft.Graph" Version="4.54.0" />
<PackageReference Include="Azure.Identity" Version="1.11.4" />

<!-- JSON -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
