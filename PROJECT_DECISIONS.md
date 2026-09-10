# SkyShift — Decision Log

תיעוד ההחלטות האדריכליות שאושרו במהלך הפרויקט, לפי סדר קבלתן. קובץ זה מתעדכן בסוף כל שכבה מאושרת, לא לפני.

## זהות הפרויקט

- השם העסקי/הרעיוני של המיזם: **"עולים לאוויר"**.
- השם הטכני הרשמי של הפרויקט (Solution, Namespaces, שמות Projects): **SkyShift** — `SkyShift.Api`, `SkyShift.Application`, `SkyShift.Domain`, `SkyShift.Infrastructure`.
- שינוי השם היה שינוי טכני בלבד; לא גרר שום שינוי אדריכלי נוסף.

## עקרונות תהליך מחייבים

- הפרויקט נבנה שכבה אחת בכל פעם, בסדר: Domain → Application → Infrastructure → Presentation/API → Dependency Injection והרצה → Testing/Architecture Check.
- לכל שכבה: ANALYZE → PRESENT PLAN → WAIT FOR EXPLICIT APPROVAL → IMPLEMENT → VERIFY (Architecture Check) → STOP.
- לא נוצר Entity, Value Object, Domain Service, Exception, Interface או Class כלשהו ללא אישור מפורש מראש.
- הסברים והחלטות אדריכליות — בעברית. קוד, שמות Class/Interface/Namespace/Project — באנגלית.

## החלטות שאושרו — שכבת Domain

**Aggregate Roots:** `User`, `Vehicle`, `VehicleAssignment`, `Flight` — ארבעה Aggregates עצמאיים, מקושרים אך ורק דרך Id (`UserId`, `VehicleId`), ללא Navigation Properties ביניהם.

**`Vehicle` ו-`VehicleAssignment` הם Aggregates נפרדים ולא מוזגו.** `VehicleAssignment` מייצג את תקופת הניסיון החודשית (עם `StartDate`/`EndDate`) ונשמר בנפרד מ-`Vehicle` כדי לאפשר היסטוריית שיוכים, לא רק את השיוך הפעיל. השמירה על עקביות בין השניים (שיוך פעיל ⇔ `Vehicle.Status == InUse`) היא Invariant שחוצה Aggregates ותוגדר ותוגן במפורש כשנגיע ל-Application (Use Case Orchestration) ולאחר מכן ל-Infrastructure/Persistence.

**`Reserved` הוסר מ-`VehicleStatus` ואינו חלק מה-MVP בשלב זה.** לא נמצא לו כלל עסקי נתמך בדרישות הנוכחיות (המעבר תמיד Available→InUse ישירות). ה-Enum הנוכחי: `Available`, `InUse`, `Maintenance`, `Unavailable`. אם בעתיד יתגלה כלל עסקי אמיתי שדורש מצב ביניים — יוצג לאישור לפני הוספתו מחדש.

**`LotteryEligibilityService` נשאר Domain Service, לא מתודה על `User`.** ההחלטה נבעה מכך שהחישוב תלוי בנתונים משני Aggregates נפרדים (`User.AverageDailyTrips` ו-מספר ה-`Flight`ים שהושלמו), ולכן שיוכו ל-`User` בלבד היה יוצר צימוד לא-רצוי בין Aggregates. הסף (80%) קבוע בשם `RequiredPercentageThreshold`, ההשוואה מתבצעת בכפל-צולב ללא חלוקה, וקיים Guard על `completedFlights` שלילי.

**לא נוצר `FlightPercentage` (או כל Value Object אחר) בשלב זה.** נבדק ונמצא שאין לו כרגע אחריות עסקית עצמאית שמצדיקה Class נפרד — החישוב מתבצע כפעולה חד-פעמית בתוך `LotteryEligibilityService`. אם בהמשך יתגלה צורך אמיתי (שימוש חוזר, ולידציה עצמאית) — יוצג להחלטה לפני יצירתו.

**Concurrency / Double Assignment של `Vehicle` לא נפתר בשכבת ה-Domain.** `Vehicle.Assign()` אוכף את כלל השיוך היחיד ברמת ה-State (מעבר חוקי רק מ-Available) עבור קריאה סדרתית מול Instance טעון אחד. הגנה מפני שני בקשות שרצות בו-זמנית (Race Condition) **טרם הוכרעה היכן תמוקם** — הנושא ייבחן ויוגדר כשנגיע ל-Application (Contract נדרש) ולאחר מכן בפועל ב-Infrastructure/Persistence (Optimistic Concurrency / DB Constraint / טרנזקציה). זו נקודה פתוחה, לא החלטה סגורה.

**`PasswordHash` ב-`User`:** ה-Entity מחזיק אך ורק `string PasswordHash`, ללא כל ידע על אלגוריתם Hashing. `IPasswordHasher` יוגדר ב-Application וימומש ב-Infrastructure.

**קבצי Domain שנוצרו (8, ללא יותר):** `Entities/User.cs`, `Entities/Vehicle.cs`, `Entities/VehicleAssignment.cs`, `Entities/Flight.cs`, `Enums/VehicleStatus.cs`, `Services/LotteryEligibilityService.cs`, `Exceptions/DomainException.cs`, `Exceptions/VehicleNotAvailableException.cs`.

## Future Feature — מחוץ ל-MVP

`AIProfile`, `Training`, `Hub`, `Reward / SkyCoinTransaction` — לא חלק מה-MVP הנוכחי. לא נוצר עבורם קוד, Interface או Class. יישקלו מחדש רק אם/כאשר יוגדר עבורם כלל עסקי ברור.

## החלטות שאושרו — שכבת Application

**Application — מאושר סופית**, לאחר ANALYZE → PLAN → אישור מפורש → IMPLEMENT → VERIFY.

**7 Use Cases נפרדים, ללא איחוד ל-Services גנריים:** `RegisterUser`, `LoginUser`, `GetAvailableVehicles`, `SelectVehicle`, `GetUserVehicle`, `RecordFlight`, `GetUserDashboard`.

**5 Repository Interfaces + `IPasswordHasher`:** `IUserRepository`, `IVehicleRepository`, `IVehicleAssignmentRepository`, `IFlightRepository`, `IPasswordHasher` — כולם ב-`Application/Abstractions`, ממומשים בהמשך ב-Infrastructure.

**8 DTOs מאושרים:** `RegisterUserRequest`, `RegisterUserResponse`, `LoginRequest`, `LoginResponse` (ללא Token), `VehicleResponse`, `SelectVehicleRequest`, `RecordFlightRequest`, `DashboardResponse`. `SelectVehicle`/`RecordFlight` ללא Response DTO ייעודי — מחזירים `Task`, הצלחה = סיום ללא Exception.

**לא נוצרו:** `Result`/`Result<T>`/`OperationResult` גנרי, `IUnitOfWork`, `ITokenGenerator`, מימוש JWT, או כל Abstraction נוסף שלא אושר במפורש.

**חישוב האחוז נשאר ב-Domain.** נוספה מתודה ציבורית `LotteryEligibilityService.CalculateCompletionPercentage(User, int)`, לצד `IsEligible` הקיים — זהו השינוי היחיד שאושר לשכבת Domain בהקשר של Application. Application לעולם לא מחשב אחוז בעצמו.

**"Completed Flights" נספר אך ורק בתוך תקופת ה-`VehicleAssignment` הפעילה** (`StartDate`–`EndDate`), לא לכל הזמן ולא לפי חודש קלנדרי. `IFlightRepository.GetCompletedCountAsync` מקבל את הטווח כפרמטר; Application מספק את הטווח, לא מחשב אותו בעצמו.

**Concurrency נשארת אחריות Infrastructure/Persistence.** הוכרע סופית ש-Application לא פותר זאת — הוא רק מגדיר את הצורך (שתי כתיבות ב-`SelectVehicle` שצריכות להצליח יחד). המנגנון בפועל ייקבע כשנגיע ל-Infrastructure.

**JWT/Token נשאר החלטה עתידית נפרדת.** `LoginUser` מאמת פרטי התחברות ומחזיר זהות בלבד; לא נוצר `ITokenGenerator`, לא הוכרע JWT מול מנגנון אחר. יידון לפני מימוש Authentication בפועל.

**מוסכמת Exceptions — אושרה:** `InvalidOperationException` למצבים לא-חוקיים/הפרת Precondition (כבר יש רכב פעיל, אין Assignment פעיל, Email רשום, פרטי התחברות שגויים). `KeyNotFoundException` כשישות מבוקשת לפי Id לא נמצאה (Vehicle, User). לא נוצר Exception Class חדש.

**קבצי Application שנוצרו (20):** `Abstractions/` (5 Interfaces), `DTOs/` (8 קבצים), `UseCases/` (7 קבצים, אחד לכל Use Case בתיקייה נפרדת).

## החלטות שאושרו — שכבת Infrastructure

**Infrastructure — מאושר סופית (FINAL APPROVED)**, לאחר ANALYZE → PLAN → אישור מפורש → IMPLEMENT → Code Review (זיהה שני פערים) → שני Fix מאושרים וממוקדים → VERIFY סופי → Build מקומי תקין (Visual Studio: 4 succeeded, 0 failed) → אישור מפורש סופי.

**מבנה ופרויקט:** `SkyShift.Infrastructure` הוא Class Library (`net8.0`) עם `ProjectReference` ל-`SkyShift.Domain` ול-`SkyShift.Application` בלבד. אחריותו: מימוש כל ה-Interfaces שהוגדרו ב-`Application/Abstractions` (Persistence, Repositories, Password Hashing), ורישום ה-Composition Root שלו ב-DI. כיוון התלות נשמר: Domain ← Application ← Infrastructure — Domain ו-Application אינם מכירים את Infrastructure.

**חבילות NuGet מאושרות (3, ללא יותר), כולן `Version="8.0.0"`:** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Identity.Core`. `Microsoft.Data.SqlClient` (משמש לזיהוי `SqlException` בתרגום שגיאות Concurrency) מגיע באופן Transitive דרך `Microsoft.EntityFrameworkCore.SqlServer` — לא נוספה חבילה רביעית.

**EF Core / SQL Server — ארכיטקטורת Persistence:** `SkyShiftDbContext` הוא `DbContext` יחיד, עם `DbSet` אחד לכל Aggregate Root (`Users`, `Vehicles`, `VehicleAssignments`, `Flights`). קונפיגורציית המודל מתבצעת אך ורק דרך Fluent API — `IEntityTypeConfiguration<T>` נפרד לכל Entity (`UserConfiguration`, `VehicleConfiguration`, `VehicleAssignmentConfiguration`, `FlightConfiguration`), נטענים דרך `ApplyConfigurationsFromAssembly`. לא נעשה שימוש ב-EF Attributes בתוך Domain — Domain נשאר "טהור" ולא מודע ל-EF Core.

**`Vehicle.Status` נשמר כ-`string` במסד הנתונים** (`HasConversion<string>()`, `MaxLength(20)`) — לא כ-`int`, כדי שערכי ה-Enum יהיו קריאים וברי-שינוי-סדר בבטחה מול הנתונים הקיימים.

**`RowVersion` על `Vehicle` — Shadow Property, לא Property מוצהר ב-Domain.** מוגדר ב-`VehicleConfiguration` בלבד (`builder.Property<byte[]>("RowVersion").IsRowVersion()`), כדי שה-Concurrency Token יישאר עניין טכני של Persistence ולא ידלוף ל-`Vehicle` עצמו.

**קשרים בין Aggregates — FK בלבד, ללא Navigation Properties, בהתאם להחלטת Domain.** כל קשר מוגדר כ-`HasOne<T>().WithMany()` על בסיס שדה ה-Id בלבד (`UserId`, `VehicleId`), ללא Navigation Property בכיוון ההפוך ובלי טעינה מרומזת בין Aggregates.

**`DeleteBehavior.Restrict` על כל הקשרים, ללא יוצא מהכלל.** נמנעת מחיקה מדורגת (Cascade) שיכולה למחוק היסטוריה (Flights, VehicleAssignments) כתוצאה ממחיקת User/Vehicle.

**אינדקסים מאושרים:** Unique Index על `User.Email`. Composite Index על `Flight(UserId, Date)`. Index על `VehicleAssignment.UserId`.

**אחריות ה-Repositories ו-Interfaces מאושרים — ללא שינוי מה-Application:** `UserRepository`, `VehicleRepository`, `VehicleAssignmentRepository`, `FlightRepository` — כל אחד ממש בדיוק את ה-Interface שהוגדר ב-Application (`IUserRepository`, `IVehicleRepository`, `IVehicleAssignmentRepository`, `IFlightRepository`), ללא הוספת מתודה או שינוי חתימה.

**`IUnitOfWork` מומש כפי שהוגדר ב-Application, ללא הרחבה:**
```csharp
public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
```
זהו החבר היחיד שנחשף ל-Application; מנגנון ה-Transaction בפועל נשאר עניין פנימי-בלבד של Infrastructure.

**ארכיטקטורת Transaction ("Option A", מאושרת):**
- כל מנגנון ה-Transaction נשאר כלוא לחלוטין בתוך Infrastructure — Application לא מכיר `DbContext`, `IsolationLevel`, או כל פרט מימוש אחר.
- הגנת Serializable מיושמת אך ורק על נתיב הליבה הקריטי של `SelectVehicle`: `VehicleAssignmentRepository.GetActiveForUserAsync`/`AddAsync` ו-`VehicleRepository.GetByIdAsync`/`UpdateAsync` — אלה בלבד קוראים ל-`SkyShiftDbContext.EnsureTransactionAsync()` (Internal, Idempotent).
- `EnsureTransactionAsync()` **אינו** נקרא באופן גורף מכל Repository. `UserRepository`, `FlightRepository`, ו-`VehicleRepository.GetAvailableAsync` פועלים ללא Transaction Serializable, מכיוון שאינם חלק מנתיב ה-Race Condition.
- `UnitOfWork.SaveChangesAsync()` הוא המקום היחיד שמבצע `CommitTransactionAsync()`/`RollbackTransactionAsync()`.
- `IUnitOfWork` לא הורחב כדי לחשוף את מנגנון ה-Transaction.
- Trade-off מתועד: Use Cases שהם קריאה-בלבד ועוברים דרך אותם Repository methods (`GetUserDashboard`, `GetUserVehicle`) פותחים גם הם את הטרנזקציה, מבלי לסגור אותה במפורש — היא משתחררת באופן מרומז עם סגירת ה-`DbContext`/Connection בסוף ה-DI Scope.

**Concurrency — שני מנגנוני הגנה נפרדים, שניהם מתורגמים ל-`VehicleNotAvailableException` הקיים (לא נוצר Exception חדש):**
- **הגנה מפני שני משתמשים על אותו Vehicle** — `RowVersion` על `Vehicle`. כשלון מסוג `DbUpdateConcurrencyException` מתורגם, בקריאת הסטטוס העדכני מה-DB (`GetDatabaseValuesAsync`), ל-`VehicleNotAvailableException`.
- **הגנה מפני שני בקשות `SelectVehicle` בו-זמניות של אותו משתמש** — הטרנזקציה המשותפת ה-Serializable שנפתחת ב-`GetActiveForUserAsync` וממשיכה עד ל-`SaveChangesAsync`. כשלון מסוג `DbUpdateException` שמקורו קוד SQL Server 1205 (Deadlock) או 1222 (Lock Timeout) — מזוהה במפורש ע"י `IsSelectVehicleSerializationConflict` — מתורגם גם הוא ל-`VehicleNotAvailableException`.
- התרגום ממוקד ומזוהה בלבד: `DbUpdateException`/`DbUpdateConcurrencyException`/`SqlException` שאינם תואמים לאחד משני התרחישים הללו **אינם** מתורגמים, ונזרקים כפי שהם.

**ניקוי Transaction — אמין תחת כל נתיב כשלון.** `_currentTransaction` מאופס ל-`null` לפני ביצוע ה-`Commit`/`Rollback` בפועל, כך שאינו יכול להישאר עם הפניה שגויה גם אם ה-Commit/Rollback עצמו נכשל. Rollback מובטח ע"י `finally`/`catch` גם כאשר קריאת ערך עדכני מה-DB (לצורך דיווח הסטטוס בחריגה) נכשלת בעצמה.

**Password Hashing:** `PasswordHasher` (Infrastructure) עוטף את `PasswordHasher<User>` מ-`Microsoft.AspNetCore.Identity` (PBKDF2, מהחבילה המאושרת `Microsoft.Extensions.Identity.Core`) — לא BCrypt ולא מימוש הצפנה עצמאי.

**DI — Composition Root יחיד:** `AddInfrastructure(this IServiceCollection services, string connectionString)` (מקבל Connection String כערך Primitive, לא `IConfiguration` — קריאת ה-Connection String בפועל נשארת אחריות עתידית של `SkyShift.Api`). רישומים: `SkyShiftDbContext` (`AddDbContext` עם `UseSqlServer`), `IUnitOfWork`, ארבעת ה-Repositories, ו-`IPasswordHasher` — כולם `Scoped`.

**קבצי Infrastructure שנוצרו:** `Persistence/SkyShiftDbContext.cs`, `Persistence/UnitOfWork.cs`, `Persistence/Configurations/{User,Vehicle,VehicleAssignment,Flight}Configuration.cs`, `Repositories/{User,Vehicle,VehicleAssignment,Flight}Repository.cs`, `Security/PasswordHasher.cs`, `DependencyInjection.cs`.

**Migrations, Connection String בפועל, ו-Database Creation — לא בוצעו בשלב זה.** אלה יידונו כשנגיע לשכבת ה-API/הרצה, ואינם חלק מהאישור הנוכחי.

## מצב נוכחי

Domain — **מאושר סופית** (לאחר Code Review).
Application — **מאושר סופית** (לאחר Code Review והרחבת Domain המאושרת).
Infrastructure — **מאושר סופית (FINAL APPROVED)**, לאחר Code Review, שני Fix ממוקדים (תרגום שגיאות Concurrency + צמצום היקף ה-Transaction), אימות סופי, ו-Build מקומי תקין.
השלב הבא: **ANALYZE API** — טרם החל. לא בוצע Program.cs, Controllers, Endpoints, Middleware, Authentication, JWT, Swagger, או Migrations.
