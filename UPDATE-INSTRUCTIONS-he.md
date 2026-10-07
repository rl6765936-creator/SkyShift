# SkyShift — עדכון הבקנד

העדכון מיועד לפרויקט המקורי ב־.NET 8 ו־EF Core 8.0.0. גרסאות הספריות לא שונו.
קובץ ההרצה המקומי של EF מוצמד ל־8.0.0 כדי לא להחליף את הכלי הגלובלי שלך.

## התקנה והרצה מקומית

1. שמרי את תיקיית הבקנד המקורית ואת גיבוי מסד הנתונים.
2. חלצי את הפרויקט החדש לתיקייה נפרדת. הגדרות החיבור הקיימות נכללות כפי שהיו בקובץ ששלחת.
3. עצרי את הבקנד הישן. פתחי טרמינל בתיקייה שבה נמצא SkyShift.sln.
4. הריצי:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet tool restore
dotnet ef database update --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api
dotnet run --project src/SkyShift.Api
```

יש להחיל את המיגרציה לפני הפעלת הבקנד החדש, משום שהוא קורא את השדות החדשים.
אין צורך להעלות לשרת. כתובת הבקנד נשארת לפי launchSettings.json הקיים.

אם המיגרציה מדווחת על כפילויות, היא נעצרת לפני שינוי הטבלאות ואינה מוחקת נתונים.
לבירור הכפילויות במסד הנתונים:

```sql
SELECT UserId, [Date], COUNT_BIG(*) AS ReportCount
FROM Flights
GROUP BY UserId, [Date]
HAVING COUNT_BIG(*) > 1;
```

אין למחוק דיווחים באופן אוטומטי; יש לבדוק את הרשומות לפני שממשיכים.

## חוזי API — התוספות

הרשמה: gender הוא שדה אופציונלי מסוג Male או Female. גם male/female מתקבלים.
חשבון ישן או לקוח שלא שלח שדה זה יקבל gender: null; לא הומצא עבורו מין.
תשובות ההרשמה וההתחברות כוללות כעת gender.

```json
{
  "name": "Example",
  "email": "example@example.com",
  "password": "example-password",
  "averageDailyTrips": 3,
  "gender": "Female"
}
```

הוספת רכב דרך POST /api/manager/vehicles, עם טוקן מנהל:

```json
{
  "isPersonalExperimental": false,
  "modelName": "SkyShift Azure SUV",
  "maxSpeedKmh": 180,
  "seats": 4,
  "imageUrl": "/assets/azure.webp"
}
```

הערכים בדוגמה להמחשה; יש לשלוח מפרט ותמונה שבחרת בפועל.
כל השדות החדשים אופציונליים כדי לשמור על קריאות ההוספה הישנות.
רכבים ישנים נשארים עם פרטים לא ידועים (null). לא נוצרו להם שמות או מפרטים רנדומליים.
הנתונים נשמרים לכל רכב עם המזהה שלו, ומוחזרים גם ברשימת הפנויים וברכב האישי.
imageUrl הוא קישור לתמונה; הקוד אינו מעלה תמונות או יוצר קובצי תמונה.
נתיב שמתחיל ב־/ מתייחס לשרת שממנו הפרונטנד מציג אותו, ויש להציב שם את הקובץ.
השינוי אינו יוצר טבלת דגמים נפרדת או מגביל כמה רכבים יכולים להשתייך לאותו דגם.

דיווח טיסה דרך POST /api/flights:

```json
{"date":"2026-10-06T12:30:00Z"}
```

יש לשלוח זמן UTC באמצעות toISOString() כמו בפרונטנד הקיים.
מותר לדווח רק עם הקצאה פעילה, על מועד שנמצא בין StartDate ו־EndDate, כולל גבולותיהם, ואינו בעתיד.
חודש הניסיון נשאר AddMonths(1), כולל השעה; הוא אינו מוחלף ב־30 ימים.
המשתמש והרכב נלקחים מהטוקן ומההקצאה, ולא מבקשת הלקוח.
כפילות מוגדרת כאותו משתמש ואותו מועד מדויק. שינוי שעה או שנייה אינו הוכחת דיווח אמיתי.

## שגיאות חדשות

| code | HTTP | משמעות |
|---|---|---|
| flight_time_zone_required | 400 | חסר זמן UTC |
| flight_outside_trial | 400 | מחוץ לתקופת ההקצאה |
| flight_in_future | 400 | טיסה בעתיד |
| duplicate_flight | 409 | דיווח כפול באותו מועד |
| invalid_vehicle_catalog | 400 | פרטי רכב לא תקינים |

התשובות שומרות על ProblemDetails ומוסיפות code, messageHe ו־messageEn.
כאשר Accept-Language מתחיל ב־he, גם detail של שגיאות אלו מוחזר בעברית.
בפרונטנד יש לבחור את messageHe/messageEn לפי השפה, או לשלוח Accept-Language: he/en.
הקוד הקיים של שגיאות אחרות לא הוחלף במסגרת עדכון זה.

## חיבור הפרונטנד

זהו עדכון לבקנד בלבד. בפרונטנד נדרש לחבר את שדה המין לטופס ההרשמה ולשמור את gender המוחזר מהשרת,
ולצרוך את modelName, maxSpeedKmh, seats, imageUrl במקום מיפוי ההמחשה המקומי.
הפרונטנד הישן נשאר תואם, אך עד לחיבור זה השדות החדשים לא ישמשו בו באופן מלא.

## בדיקות

נוספו בדיקות לתאריכים ולגבולות חודש, לכפילויות, לשיוך משתמש ורכב, למין, למפרטי רכב,
לייחודיות האינדקס, להתאמת המיגרציה למודל ולסדר בדיקת הנתונים לפני שינוי הסכמה.
הבדיקות הקיימות לבחירת רכב ול־RowVersion נשמרו.

בדיקות SQL Server נפרדות יוצרות ומוחקות מסד בדיקה חדש בשם SkyShiftTests_<מזהה>.
הן אינן משנות את מסד הנתונים ששמו מופיע במחרוזת החיבור.
נדרש משתמש SQL עם הרשאה ליצור ולמחוק מסדי בדיקה. אין למסור סיסמאות בצ'אט.

```powershell
$env:SKYSHIFT_TEST_SQLSERVER = "<connection string to your test SQL Server>"
dotnet test --filter "Category=SqlServer"
Remove-Item Env:SKYSHIFT_TEST_SQLSERVER
```

בלי משתנה זה שתי בדיקות האינטגרציה מסומנות כ־Skipped.
