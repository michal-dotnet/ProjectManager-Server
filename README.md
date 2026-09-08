# projectManager2 — ניהול תוכניות עבודה לאירועים

מערכת לניהול אירועים ומשימות: מנהל (Manager) יוצר אירוע ומוסיף אליו משימות וחברי צוות, וחברי הצוות (Worker) "לוקחים" משימות פנויות לביצוע.

## ארכיטקטורה

פרויקט מחולק לשכבות, כל אחת ב-Project נפרד:

| Project | תפקיד |
|---|---|
| `projectManager2` | ה-Web API (Controllers, Program.cs, Middlewares) |
| `projectManager2Core` | חוזה משותף: Models, DTOs, Interfaces, Exceptions — בלי תלות בשום חבילה חיצונית |
| `projectManager2Data` | גישה למסד הנתונים: EF Core, DataContext, Repositories, Migrations |
| `projectManager2Service` | הלוגיקה העסקית: Services, AutoMapper, JWT |
| `projectManager2Tests` | בדיקות (xUnit + Moq + Sqlite) |
| `projectManager2Client` | צד לקוח: React + TypeScript + Vite + Tailwind |

## מסד הנתונים — PostgreSQL

הפרויקט משתמש ב-**PostgreSQL** (לא SQL Server) — כדי לעבוד עם מסדים מנוהלים בחינם כמו זה של [Render](https://render.com). צריך מסד Postgres זמין (מקומי, או בענן) לפני ההרצה הראשונה.

**הכי פשוט:** ליצור מסד Postgres בחינם ב-Render (New → PostgreSQL → תוכנית **Free**), ולהשתמש באותו Connection String גם לפיתוח מקומי וגם בהמשך לפריסה — כך שלא צריך להתקין שום דבר על המחשב. שימו לב: מסד חינמי ב-Render **פג תוקף אחרי 30 יום**.

## הגדרות סודות (User Secrets)

לפני ההרצה הראשונה, לחיצה ימנית על פרויקט `projectManager2` ← **Manage User Secrets**, ולהגדיר:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<Hostname>;Port=5432;Database=<Database>;Username=<Username>;Password=<Password>;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Jwt": {
    "Key": "מפתח-סודי-ארוך-לפחות-32-תווים"
  }
}
```

את הפרטים (`Hostname`/`Database`/`Username`/`Password`) לוקחים מלשונית **Connect** בדף המסד ב-Render (External Database URL / הפרטים בנפרד). `SSL Mode=Require` הכרחי — Render דורש חיבור מוצפן מבחוץ.

לאחר מכן, ב-**Package Manager Console** (Default Project: `projectManager2Data`):

```
Update-Database
```

## הרצת המערכת

**1. ה-API** — ב-Visual Studio, לוודא שהפרופיל הנבחר הוא **http** (לא https, כדי למנוע התנגשות CORS/תעודות), ולהריץ (F5). עולה על `http://localhost:5080`. Swagger זמין ב-`http://localhost:5080/swagger`.

**2. ה-Client** — בטרמינל, בתוך `projectManager2Client`:

```
npm install
npm run dev
```

עולה על `http://localhost:5173`. יש להריץ קודם את ה-API, ורק אז את ה-Client.

## משתמשי דמו

נטענים אוטומטית בהרצה הראשונה (Seed Data):

| Role | Email | סיסמה |
|---|---|---|
| Manager | `manager@projectmanager2.local` | `Manager@12345` |
| Worker | `worker@projectmanager2.local` | `Worker@12345` |

## הרשאות ומצב אירוע

- **Manager** — יוצר אירועים, מוסיף אליהם משימות וחברים, ומסמן אירוע כ-Completed.
- **Worker** — רואה אירועים שהוא חבר בהם, ולוקח (Take) משימות פנויות.
- כל אירוע נוצר ישירות במצב **Active**. אירוע **Completed** הוא Read-Only — לא ניתן עוד לערוך אותו, להוסיף חברים/משימות, או לקחת ממנו משימות.
- רק **Event Creator** (מי שיצר את האירוע) יכול לערוך אותו, להוסיף/להסיר חברים, ולסמן אותו כ-Completed.

## תכונות טכניות עיקריות

- **אימות** — JWT (הרשמה/התחברות דרך `/api/auth`), עם בחירת Role עצמאית (Manager/Worker) בהרשמה.
- **Pagination** — כל רשימה (`GET` על Events/Users/Tasks/Members) מדופדפת (`pageNumber`, `pageSize`), ומחזירה גם `totalCount`/`totalPages`.
- **AutoMapper** — מיפוי Entity↔DTO מרוכז ב-`projectManager2Service/Mapping/MappingProfile.cs`.
- **הגנת Concurrency** — "לקיחת" משימה (Take Task) מוגנת ב-Concurrency Token אמיתי של EF Core (לא נעילה ידנית): אם שני משתמשים מנסים לקחת אותה משימה בו-זמנית, רק אחד מצליח והשני מקבל שגיאת 409 Conflict ברורה.
- **CORS** — מוגדר במפורש לכתובת ה-Client (`http://localhost:5173`) בלבד.

## בדיקות

**Test Explorer** ב-Visual Studio (Ctrl+E, T) ← Run All. כולל:

- בדיקת Concurrency אמיתית מול Sqlite (שני משתמשים לוקחים אותה משימה בו-זמנית).
- בדיקות יחידה (Moq) ללוגיקה העסקית של השירותים — הרשאות, מצבי אירוע, ולידציות.

## פתרון תקלות נפוצות

- **CS0006 / שגיאות Build מוזרות** — Build ← Clean Solution, ואז Rebuild Solution.
- **CORS Error בדפדפן** — לוודא שה-API רץ עם פרופיל **http** (לא https).
- **`npm run dev` נכשל על Execution Policy** — להריץ פעם אחת: `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned`.
- **שגיאת חיבור למסד (`Received unknown response ... for SSLRequest`)** — פורט 5432 (Postgres) חסום/מסונן ברשת (פיירוול, אנטי-וירוס עם בדיקת HTTPS, או שירות סינון תוכן ברמת ה-ISP). לבדוק חיבור TCP גולמי עם `Test-NetConnection -ComputerName <Host> -Port 5432`; אם החיבור עצמו עובר (`TcpTestSucceeded: True`) אבל השגיאה נשארת, הבעיה היא סינון תוכן/אבטחה שדורש חריג ספציפי לכתובת ולפורט הזה.
