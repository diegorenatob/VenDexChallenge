namespace VendSys.Maui.Models;

public sealed record ApiResult(bool IsSuccess, string? ErrorMessage)
{
    public static ApiResult Success() => new(true, null);
    public static ApiResult Failure(string errorMessage) => new(false, errorMessage);
}
