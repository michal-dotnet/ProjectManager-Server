// ראו הסבר מלא ב-projectManager2Core/TaskAliases.cs. הפרויקט הנוכחי מקושר
// ל-Core ומשתמש גם ב-Entity Task וגם ב-async Task, ולכן זקוק לאותם aliases
// (global using הוא per-assembly ואינו "עובר" אוטומטית בין פרויקטים).
global using TaskEntity = projectManager2Core.Models.Task;
global using TaskStatusEnum = projectManager2Core.Enums.TaskStatus;
