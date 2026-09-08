using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;
using projectManager2.Helpers;
using projectManager2.Middlewares;
using projectManager2Core.Repositories;
using projectManager2Core.Services;
using projectManager2Data;
using projectManager2Service;
using projectManager2Service.Mapping;

var builder = WebApplication.CreateBuilder(args);

// NLog מחליף את ספק ה-Logging המובנה של ASP.NET Core. הקוד בכל שכבות
// האפליקציה (Controllers/Services/Middlewares) ממשיך להזריק ולהשתמש רק
// ב-ILogger<T> הרגיל של Microsoft.Extensions.Logging; ההחלפה בפועל
// מתבצעת אך ורק כאן, בשורש ה-Composition (Program.cs). התצורה עצמה
// (רמות, יעדים - קובץ/קונסול) מוגדרת ב-nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums (EventStatus, TaskStatus, UserRole) מוצגים כמחרוזות קריאות
        // ב-JSON (למשל "Active" במקום 1) - נוח יותר לצריכה מצד ה-Client.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// AddOpenApi() לא מוסיף לבד סכימת אימות ל-Swagger - בלי ה-Transformer הבא
// לא היה מופיע כפתור "Authorize" בכלל, ולא ניתן היה לבדוק Endpoint מוגן
// (401) ידנית דרך Swagger UI. מוסיף כאן הגדרת Bearer JWT גלובלית שחלה על
// כל ה-Endpoints (גם אלה שלא דורשים אימות - לא מזיק).
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "הכניסו רק את ה-Token עצמו (בלי המילה Bearer) - Swagger UI מוסיף אותה לבד."
        };

        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
            }] = Array.Empty<string>()
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddHttpContextAccessor();

// CORS: מאפשר לצד הלקוח (React, Vite Dev Server - רץ על פורט אחר) לקרוא
// ל-API. מוגדר מפורש (לא AllowAnyOrigin) - רק המקורות של שרת הפיתוח של
// Vite, כולל Authorization Header (ה-JWT) ו-Content-Type.
const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// שכבת ה-Data: רישום ה-DbContext. מחרוזת ההתחברות מגיעה מ-User Secrets
// בפיתוח (ConnectionStrings:DefaultConnection) - לא נשמרת ב-appsettings.json
// שנכנס ל-Repo. לחיצה ימנית על הפרויקט -> Manage User Secrets. בענן (Render)
// אותו מפתח מוגדר כ-Environment Variable, לא כ-User Secrets.
//
// PostgreSQL (Npgsql) ולא SQL Server - כדי להשתמש במסד המנוהל בחינם של
// Render (אין להם SQL Server מנוהל). ה-Concurrency Token (TaskEntity.
// ConcurrencyVersion) הוגדר מלכתחילה כ-int רגיל ולא כ-rowversion, כך
// שהמעבר בין Providers לא דורש שום שינוי בלוגיקת ה-Concurrency עצמה.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection אינו מוגדר. הריצי 'Manage User Secrets' על פרויקט ה-Api והגדירי אותו שם (ראו README).");

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories: ממשקי Core -> מימושים ב-Data.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventMemberRepository, EventMemberRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Services: ממשקי Core -> מימושים ב-Service.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventMemberService, EventMemberService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// AutoMapper (נקודה 4): מיפוי Entity -> DTO מוגדר ב-MappingProfile
// (projectManager2Service.Mapping) במקום מתודות ToDto ידניות בכל Service.
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// זהות המשתמש הנוכחי נשלפת מה-Claims של ה-JWT המאומת (ראו CurrentUserService
// ו-Middleware האימות למטה).
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// JWT Authentication: מפתח החתימה (Jwt:Key) חייב להגיע מ-User Secrets
// בפיתוח - לעולם לא מקודד כאן ולא נשמר ב-appsettings.json שנכנס ל-Repo.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key אינו מוגדר. הריצי 'Manage User Secrets' על פרויקט ה-Api והגדירי שם ערך ל-Jwt:Key (ראו README).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "projectManager2";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "projectManager2Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Migrate + Seed Data - רצים אוטומטית בכל עליית האפליקציה, בתוך התהליך
// עצמו (לא ידנית מ-Package Manager Console). זה הכרחי בסביבת הענן (Render):
// שם האפליקציה עצמה מתחברת למסד מתוך התשתית של Render, בלי לעבור דרך
// שום רשת חיצונית - כך שהמסד "מוכן" גם בלי צורך בהרצת Update-Database
// ידנית ממחשב מפתחת כלשהו. MigrateAsync אידמפוטנטי: מריץ רק Migrations
// שעוד לא הוחלו, ולא עושה כלום אם המסד כבר מעודכן.
using (var migrationScope = app.Services.CreateScope())
{
    var context = migrationScope.ServiceProvider.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
    await DataSeeder.EnsureSeededAsync(context);
}

// CorrelationId - ראשון בפייפליין (אפילו לפני Exception Handling), כדי
// שה-CorrelationId יהיה זמין ב-Log של כל חריגה שנתפסת בהמשך.
app.UseMiddleware<CorrelationIdMiddleware>();

// Global Exception Handling - שני בפייפליין כדי לתפוס חריגות מכל מה שאחריו,
// וממפה אותן ל-ProblemDetails עקבי בלי לחשוף Stack Trace.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // מסמך ה-OpenAPI זמין ב-/openapi/v1.json
    app.MapOpenApi();

    // ממשק ויזואלי (Swagger UI) שקורא את אותו מסמך - זמין ב-/swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "projectManager2 API v1");
    });
}

app.UseHttpsRedirection();

// CORS - חייב לבוא לפני Authentication/Authorization, כדי שגם בקשות
// Preflight (OPTIONS) שהדפדפן שולח יעברו בלי להיחסם.
app.UseCors(ClientCorsPolicy);

// Authentication לפני Authorization - סדר הפייפליין הנדרש: קודם מזהים מי
// המשתמש (JWT), ורק אז בודקים אם מותר לו לבצע את הפעולה ([Authorize]/
// [Authorize(Roles=...)] על ה-Controllers).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    app.Run();
}
finally
{
    // מוודא שה-Buffer של NLog מתרוקן (Flush) לפני שהתהליך נסגר - אחרת
    // השורות האחרונות עלולות "ללכת לאיבוד" בסגירה פתאומית של התהליך.
    NLog.LogManager.Shutdown();
}
