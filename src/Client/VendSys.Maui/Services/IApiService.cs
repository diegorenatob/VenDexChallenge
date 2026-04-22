using VendSys.Maui.Models;

namespace VendSys.Maui.Services;

public interface IApiService
{
    Task<ApiResult> SendDexFileAsync(string machine, string dexContent);
}
