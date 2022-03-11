﻿namespace MemberService.Services.Vipps;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using MemberService.Services.Vipps.Models;

public class AccessTokenHandler : DelegatingHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AccessTokenCache _accessTokenCache;

    public AccessTokenHandler(
        IHttpClientFactory httpClientFactory,
        AccessTokenCache accessTokenCache)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenCache = accessTokenCache;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(await GetAccessToken());

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessToken()
    {
        var accessToken = _accessTokenCache.AccessToken;

        if (accessToken?.HasExpired == false) return accessToken.AccessToken;

        var client = _httpClientFactory.CreateClient("Vipps-auth");

        var response = await client.PostAsJsonAsync("/accessToken/get", new {});
        response.EnsureSuccessStatusCode();
        accessToken = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        _accessTokenCache.AccessToken = accessToken;

        return accessToken.AccessToken;
    }
}
