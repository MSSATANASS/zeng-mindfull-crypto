using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace CoinbaseOauthDemo.Pages;

public class CoinbaseOauthModel : PageModel
{
    private const string SessionKey = "coinbase_oauth_state";
    private readonly IConfiguration _configuration;

    public CoinbaseOauthModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? State { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? AccessToken { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? AuthorizationUrl { get; private set; }
    public bool FlowCompleted => !string.IsNullOrEmpty(AccessToken) || !string.IsNullOrEmpty(ErrorMessage);

    public async Task OnGetAsync()
    {
        AuthorizationCode = Request.Query["code"];
        var returnedState = Request.Query["state"].ToString();

        if (string.IsNullOrEmpty(AuthorizationCode))
        {
            GenerateAuthorizationRequest();
            return;
        }

        State = HttpContext.Session.GetString(SessionKey);

        if (string.IsNullOrEmpty(returnedState) || string.IsNullOrEmpty(State) || !string.Equals(State, returnedState, StringComparison.Ordinal))
        {
            ErrorMessage = "El parámetro state recibido no es válido.";
            return;
        }

        HttpContext.Session.Remove(SessionKey);

        var clientId = _configuration["Coinbase:ClientId"];
        var clientSecret = _configuration["Coinbase:ClientSecret"];
        var redirectUri = _configuration["Coinbase:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
        {
            ErrorMessage = "Configura Coinbase:ClientId, Coinbase:ClientSecret y Coinbase:RedirectUri antes de continuar.";
            return;
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = AuthorizationCode!,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
        };

        try
        {
            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(tokenRequest);
            using var response = await httpClient.PostAsync("https://api.coinbase.com/oauth/token", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"Fallo al obtener el token de acceso: {(int)response.StatusCode} {response.ReasonPhrase}.";
                return;
            }

            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                AccessToken = accessTokenElement.GetString();
            }

            if (string.IsNullOrEmpty(AccessToken))
            {
                ErrorMessage = "La respuesta de Coinbase no contiene un token de acceso.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Error de red al intercambiar el código: {ex.Message}";
        }
        catch (JsonException ex)
        {
            ErrorMessage = $"No se pudo interpretar la respuesta de Coinbase: {ex.Message}";
        }
    }

    private void GenerateAuthorizationRequest()
    {
        State = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(SessionKey, State);

        var clientId = _configuration["Coinbase:ClientId"];
        var redirectUri = _configuration["Coinbase:RedirectUri"];
        var scope = _configuration["Coinbase:Scope"] ?? "wallet:user:read";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            ErrorMessage = "Configura Coinbase:ClientId y Coinbase:RedirectUri para iniciar el flujo de autorización.";
            return;
        }

        AuthorizationUrl = string.Join(string.Empty,
            "https://www.coinbase.com/oauth/authorize",
            "?response_type=code",
            "&client_id=", Uri.EscapeDataString(clientId),
            "&redirect_uri=", Uri.EscapeDataString(redirectUri),
            "&state=", Uri.EscapeDataString(State!),
            "&scope=", Uri.EscapeDataString(scope));
    }
}
