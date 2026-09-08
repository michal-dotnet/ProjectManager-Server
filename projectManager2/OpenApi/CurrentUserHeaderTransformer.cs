using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using projectManager2.Middlewares;

namespace projectManager2.OpenApi;

/// <summary>
/// מוסיף למסמך ה-OpenAPI פרמטר Header בשם X-User-Id עבור כל ה-Operations
/// שתחת /api/events (Events/Members/Tasks) - בדיוק אותם נתיבים ש-
/// שכבת האימות רשומה עליהם ב-Program.cs. כך ש-Swagger UI
/// מציג שדה קלט ל-"משתמש המאומת" ואפשר לנסות את ה-API בלי curl/Postman.
///
/// יצירת/קבלת Users (/api/users) נשארת בלי הפרמטר - שם אין אימות.
/// </summary>
internal sealed class CurrentUserHeaderTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var relativePath = context.Description.RelativePath ?? string.Empty;

        if (!relativePath.StartsWith("api/events", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-User-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "מזהה (int) של המשתמש המאומת - ה-Id שחוזר מ-POST /api/users.",
            Schema = new OpenApiSchema
            {
                Type = "integer",
                Format = "int32"
            }
        });

        return Task.CompletedTask;
    }
}
