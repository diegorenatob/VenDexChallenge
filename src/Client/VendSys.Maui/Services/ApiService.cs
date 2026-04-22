using VendSys.Maui.Models;

namespace VendSys.Maui.Services;

public sealed class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ApiResult> SendDexFileAsync(string machine, string dexContent) =>
        throw new NotImplementedException();
}
