using oneDocument.Auth.Models;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace oneDocument.Auth.Services;
public interface IAuthenticationService
{
    //Task<string> Logout(string userName);
    //Task<IEnumerable<IAccount>> GetExistingAccountsAsync();
    Task<string> GetTokenAsync(string[] scopes, bool silentOnly);
    void LogOut();
}

public static class AuthCacheConfig
{
    // App settings
    public static readonly string[] Scopes = new[] { "offline_access", "openid", "profile", "User.Read" };
    public const string Authority = "https://login.microsoftonline.com/1a407a2d-7675-4d17-8692-b3ac285306e4";
    public const string ClientId = "ddeafcf6-e23c-4c4f-bb5a-308183bd91e6";
    public const string RedirectURI = "msauth.com.philips.cl.AsBuiltGUIMobile://auth";
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    //private TokenResponseModel _token;
    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        HttpStatusCode[] httpStatusCodesWorthRetrying = {
               HttpStatusCode.RequestTimeout, // 408
               HttpStatusCode.InternalServerError, // 500
               HttpStatusCode.BadGateway, // 502
               HttpStatusCode.ServiceUnavailable, // 503
               HttpStatusCode.GatewayTimeout // 504
            };
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrInner<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
              .WaitAndRetryAsync(new[]
              {
                System.TimeSpan.FromSeconds(2),
                System.TimeSpan.FromSeconds(4),
                System.TimeSpan.FromSeconds(8)
              });
            MauiProgram._token = null;
    }

    public async Task<string> GetTokenAsync(string[] scopes, bool silentOnly)
    {
        try
        {
            await GetTokenFromStorageAsync();
            MauiProgram._token = null;
            if (MauiProgram._token != null)
            {
                if (MauiProgram._token.expires_at > DateTime.UtcNow.AddSeconds(30))
                {
                    return MauiProgram._token.access_token;
                }
                return await GetRefreshTokenAsync(scopes, silentOnly);
            }
            else
            {
                return await GetWebTokenAsync(scopes, silentOnly);
            }
        }
        catch(Exception ex)
        {
            return string.Empty;
        }
    }

    public void LogOut()
    {
        MauiProgram._token = null;
        string refreshToken;
#if MACCATALYST
        Preferences.Remove(nameof(refreshToken));
#else
        SecureStorage.Remove(nameof(refreshToken));
#endif
    }

    private async Task GetTokenFromStorageAsync()
    {
        if(MauiProgram._token == null)
        {
            string refreshToken = string.Empty;
#if MACCATALYST
            refreshToken = Preferences.Get(nameof(refreshToken), string.Empty);
#else
            refreshToken = await SecureStorage.GetAsync(nameof(refreshToken));
#endif
            if (!string.IsNullOrEmpty(refreshToken))
            {
                MauiProgram._token = new()
                {
                    refreshToken = refreshToken
                };
            }
        }
    }

    private async Task SetTokenInStorageAsync(string refreshToken)
    {
#if MACCATALYST
        Preferences.Set(nameof(refreshToken), refreshToken);
#else
        await SecureStorage.SetAsync(nameof(refreshToken), refreshToken);
#endif
    }

    private async Task<string> GetRefreshTokenAsync(string[] scopes, bool silentOnly)
    {
        //https://login.microsoftonline.com/common/oauth2/v2.0/token
        //-ContentType application/x-www-form-urlencoded -Method POST
        //&code=$code&grant_type=refresh_token
        //&refresh_token=$($oauthTokens.refresh_token)"
        List<KeyValuePair<string, string>> loginPayload = new();
        loginPayload.Add(new("client_id", AuthCacheConfig.ClientId));
        loginPayload.Add(new("scope", string.Join(" ", scopes)));
        loginPayload.Add(new("grant_type", "refresh_token"));
        loginPayload.Add(new("refresh_token", MauiProgram._token.refreshToken));
        MauiProgram._token = await GetTokenFromAzAsync(scopes, loginPayload);
        if(MauiProgram._token != null)
        {
            return MauiProgram._token.access_token;
        }
        return await GetWebTokenAsync(scopes, silentOnly);
    }

    private async Task<string> GetWebTokenAsync(string[] scopes, bool silentOnly)
    {
        if (!silentOnly)
        {
#if WINDOWS
            var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(
                new System.Uri(GenerateCodeUri(scopes)),
                new System.Uri(AuthCacheConfig.RedirectURI));
#else
                var result = await WebAuthenticator.AuthenticateAsync(
                    new System.Uri(GenerateCodeUri(scopes)), 
                    new System.Uri(AuthCacheConfig.RedirectURI));
#endif
            var code = result.Properties["code"];
            List<KeyValuePair<string, string>> loginPayload = new();
            loginPayload.Add(new("client_id", AuthCacheConfig.ClientId));
            loginPayload.Add(new("scope", string.Join(" ", scopes)));
            loginPayload.Add(new("grant_type", "authorization_code"));
            loginPayload.Add(new("code", code));
            loginPayload.Add(new("redirect_uri", AuthCacheConfig.RedirectURI));
            MauiProgram._token = await GetTokenFromAzAsync(scopes, loginPayload);
            return MauiProgram._token.access_token;
        }
        else
        {
            return string.Empty;
        }
    }

    private async Task<TokenResponseModel> GetTokenFromAzAsync( 
        string[] scopes, List<KeyValuePair<string, string>> loginPayload)
    {
        HttpResponseMessage responseMessage = await 
            _retryPolicy.ExecuteAsync(async () =>
                         await CreateMessageAndSendAsync(
                             AuthCacheConfig.Authority.TrimEnd('/') +
                                 "/oauth2/v2.0/token", loginPayload));
        if (responseMessage.IsSuccessStatusCode)
        {
            string responseText = await responseMessage.Content.ReadAsStringAsync();
            Debug.WriteLine(responseText);
            TokenResponseModel tokenResponse = JsonSerializer.Deserialize
                <TokenResponseModel>(responseText);
            if (tokenResponse  != null)
            {
                tokenResponse.expires_at = DateTime.UtcNow.AddSeconds(
                    tokenResponse.expires_in);
                await SetTokenInStorageAsync(tokenResponse.refreshToken);
                return tokenResponse;
            }
            throw new Exception("Error getting token");
        }
        else
        {
            string responseText = await responseMessage.Content.ReadAsStringAsync();
            throw new Exception(responseText);
        }
    }

    private async Task<HttpResponseMessage> CreateMessageAndSendAsync(string url,
        List<KeyValuePair<string, string>> payload)
    {
        HttpRequestMessage requestMessage = new(HttpMethod.Post,
            url)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        return await _httpClient.SendAsync(requestMessage);
    }

    private string GenerateCodeUri(string[] scopes)
    {
        return AuthCacheConfig.Authority.TrimEnd('/') +
            $"/oauth2/v2.0/authorize?client_id={AuthCacheConfig.ClientId}&scope=" +
            string.Join(" ", scopes) + 
            $"&redirect_uri={AuthCacheConfig.RedirectURI}" +
            $"&response_type=code" +
            $"&prompt=login";
    }

}