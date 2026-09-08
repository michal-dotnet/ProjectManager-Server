# Dockerfile ל-projectManager2 API - Multi-stage Build.
# שלב 1 (build): SDK מלא, בונה ומפרסם Release.
# שלב 2 (runtime): רק ה-Runtime של ASP.NET Core - Image קטן וקל יותר.
# הפרויקטים projectManager2Client ו-projectManager2Tests בכוונה לא מועתקים
# לכאן - הם לא חלק מה-API שרץ בפרודקשן.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# מעתיקים קודם רק את קבצי ה-.csproj - כדי ש-Docker ישמור בקאש את שכבת
# ה-restore, ולא יריץ אותה מחדש בכל שינוי קוד (רק כשקובץ Project משתנה).
COPY projectManager2/projectManager2.csproj projectManager2/
COPY projectManager2Core/projectManager2Core.csproj projectManager2Core/
COPY projectManager2Data/projectManager2Data.csproj projectManager2Data/
COPY projectManager2Service/projectManager2Service.csproj projectManager2Service/

RUN dotnet restore projectManager2/projectManager2.csproj

# עכשיו מעתיקים את שאר הקוד של אותם 4 פרויקטים ומפרסמים.
COPY projectManager2/ projectManager2/
COPY projectManager2Core/ projectManager2Core/
COPY projectManager2Data/ projectManager2Data/
COPY projectManager2Service/ projectManager2Service/

RUN dotnet publish projectManager2/projectManager2.csproj -c Release -o /app/publish --no-restore

# ---------- שלב ה-Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render מזריק את הפורט שהאפליקציה צריכה להאזין לו דרך משתנה סביבה בשם
# PORT, שנקבע רק בהרצה בפועל (לא בזמן Build) - לכן ASPNETCORE_URLS מוגדר
# בתוך ה-ENTRYPOINT (shell), לא כ-ENV קבוע. 8080 הוא ברירת מחדל להרצה
# מקומית של ה-Image (docker run) בלי Render בכלל.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet projectManager2.dll"]
