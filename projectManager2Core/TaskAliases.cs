// ה-Entity נקרא "Task" וה-Enum נקרא "TaskStatus" לפי דרישות המערכת.
// שני השמות מתנגשים עם טיפוסים מובנים ב-BCL (System.Threading.Tasks.Task /
// System.Threading.Tasks.TaskStatus) שנטענים אוטומטית לכל קובץ בפרויקט זה
// דרך ImplicitUsings (global using System.Threading.Tasks;).
//
// כדי לא לשנות את שמות ה-Entity/Enum (כנדרש), ובמקביל להימנע משגיאות
// קומפילציה מסוג "CS0104 ambiguous reference" בכל קובץ שמשתמש גם ב-
// async Task וגם ב-Entity/Enum, מוגדרים כאן שני aliases גלובליים לפרויקט.
// בכל מקום מחוץ ל-namespace projectManager2Core.Models (שם אין התנגשות כי
// חברי ה-namespace המקומי גוברים על using) יש להשתמש ב-TaskEntity/TaskStatusEnum
// במקום בשם החשוף Task/TaskStatus.
global using TaskEntity = projectManager2Core.Models.Task;
global using TaskStatusEnum = projectManager2Core.Enums.TaskStatus;
