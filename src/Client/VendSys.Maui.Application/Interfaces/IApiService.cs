using VendSys.Client.Application.Models;

namespace VendSys.Client.Application.Interfaces;

public interface IApiService
{
    Task<ApiResult> SendDexFileAsync(string machine, string dexContent);

    Task<ApiResult> ClearAllDataAsync();
}
