# SkyShift — תיקון אבטחת AutoMapper

חלצי את הקבצים לתוך תיקיית SkyShift-Backend הקיימת, שבה נמצא SkyShift.sln, ואשרי החלפה.
העדכון מחליף ארבעה קבצי קוד בלבד. הוא אינו כולל קובצי appsettings או מיגרציה חדשה.

מה השתנה:
- AutoMapper שודרג מ־12.0.1 ל־15.1.1, גרסה מתוקנת לפי GHSA-rvv3-g6hj-g44x.
- חבילת AutoMapper.Extensions.Microsoft.DependencyInjection הוסרה; רישום DI נמצא כעת בחבילת הליבה.
- Program מותאם לרישום החדש וקורא מפתח רישיון מהגדרה AutoMapper:LicenseKey, אם הוגדר.
- בניית MapperConfiguration בבדיקות מקבלת ILoggerFactory.
- נוספו שתי בדיקות לתקינות כל הפרופילים ולרישום DI ולמיפוי המין בהתחברות.

אימות שבוצע: כל חמשת הפרויקטים נבנו, ללא אזהרות בפלט הבנייה.
23 בדיקות עברו בהרצה ישירה, 0 נכשלו; שתי בדיקות SQL Server עדיין לא הורצו כאן.
רץ VSTest אינו תקין בסביבת העבודה הזו; יש להריץ את הבדיקות הרגילות אצלך.
לא נבדקה כאן הפעלת השרת מול מסד הנתונים המקומי שלך.

מקורות:
https://github.com/advisories/GHSA-rvv3-g6hj-g44x
https://docs.automapper.io/en/latest/15.0-Upgrade-Guide.html
https://luckypennysoftware.com/license/changes-v2

הרצה מקומית לאחר ההחלפה:
```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

לבדיקות SQL Server על השרת המקומי שלך:
```powershell
$env:SKYSHIFT_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet test --no-build --filter "Category=SqlServer"
Remove-Item Env:SKYSHIFT_TEST_SQLSERVER
```
הבדיקות יוצרות מסדי בדיקה נפרדים SkyShiftTests_<מזהה> ומוחקות אותם בסיום.
הן אינן משתמשות במסד SkyShiftDb שלך.

רישיון:
הגרסה החדשה דורשת רישיון. לפי תנאי הספק, עבודת תלמידים ושימוש ללימודים זכאים ל־Community License.
יש לקבל את המפתח דרך https://automapper.io ולשמור אותו מקומית, מחוץ לגיט:
```powershell
dotnet user-secrets set "AutoMapper:LicenseKey" "<your-key>" --project src/SkyShift.Api
```
אין לשלוח את המפתח בצ'אט או להכניס אותו לקוד/לקובצי appsettings שעולים לגיט.
אין בקוד דיכוי של הודעות רישוי. בהיעדר מפתח הספרייה עשויה להדפיס אזהרה, ואין לבלבל זאת עם אזהרת NU1903 שתוקנה בשדרוג.

לאחר הבדיקות, הפעילי שוב את הבקנד ובדקי כניסה ושליפת הדשבורד ב־Swagger.
