using VendSys.Application.UseCases;

namespace VendSys.Api.Endpoints;

/// <summary>Maps the POST /vdi-dex endpoint.</summary>
public static class DexEndpoints
{
    public static void MapDexEndpoints(this WebApplication app)
    {
        app.MapPost("/vdi-dex", HandleAsync)
           .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        ProcessDexFileUseCase useCase,
        string? machine)
    {
        if (string.IsNullOrEmpty(machine) || machine is not ("A" or "B"))
            return Results.BadRequest(new { error = "Query parameter 'machine' is required and must be 'A' or 'B'." });

        string dexText;
        using (var reader = new StreamReader(context.Request.Body))
            dexText = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(dexText))
            return Results.BadRequest(new { error = "Request body must not be empty." });

        var result = await useCase.ExecuteAsync(dexText, machine);

        return Results.Ok(result);
    }
}
