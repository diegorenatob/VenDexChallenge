using System.Net.Http.Headers;
using System.Text;
using VendSys.Client.Application.Interfaces;
using VendSys.Client.Application.Models;

namespace VendSys.Maui.Infrastructure.Services;

public sealed class ApiService : IApiService
{
    private static readonly string _encodedCredentials = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{ApiConstants.AuthUsername}:{ApiConstants.AuthPassword}"));

    private readonly IHttpClientFactory _httpClientFactory;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResult> SendDexFileAsync(string machine, string dexContent)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ApiConstants.HttpClientName);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{ApiConstants.DexEndpoint}?{ApiConstants.MachineParamKey}={machine}")
            {
                Content = new StringContent(dexContent, Encoding.UTF8, "text/plain"),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue(ApiConstants.AuthScheme, _encodedCredentials);

            var response = await client.SendAsync(request);

            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure($"Server returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }
}
