using System.Net;
using System.Net.Http.Json;
using Vowlt.Api.Features.OAuth.DTOs;
using Vowlt.Api.Tests.Infrastructure;

namespace Vowlt.Api.Tests.Features;

/// <summary>
/// Integration tests for OAuth 2.1 Authorization Code Flow with PKCE.
/// Tests real security concerns, not toy examples.
/// </summary>
/// <summary>
/// Integration tests for OAuth 2.1 Authorization Code Flow with PKCE.
/// Each test gets a clean database for proper isolation.
/// </summary>
public class OAuthFlowTests : IDisposable
{
    private readonly VowltWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OAuthFlowTests()
    {
        _factory = new VowltWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Authentication Requirements

    [Fact]
    public async Task AuthorizeEndpoint_WithoutAuthentication_Returns401()
    {
        // Arrange
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var url = BuildAuthorizeUrl("vowlt-dev-client", "http://localhost:3000/callback", codeChallenge);

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region PKCE Security Tests (CRITICAL)

    [Fact]
    public async Task TokenEndpoint_WithWrongCodeVerifier_ReturnsBadRequest()
    {
        // Arrange - Complete OAuth flow with correct PKCE
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge);

        // Generate DIFFERENT code_verifier (attacker scenario)
        var wrongCodeVerifier = PKCEHelper.GenerateCodeVerifier();

        // Act - Try to exchange with wrong verifier
        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = wrongCodeVerifier // WRONG!
        };

        var response = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);

        // Assert - Must fail PKCE validation
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_grant", errorContent);
    }

    [Fact]
    public async Task TokenEndpoint_ReusingAuthorizationCode_ReturnsBadRequest()
    {
        // Arrange - Complete OAuth flow once
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge);

        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = codeVerifier
        };

        // Act - Exchange code first time (should succeed)
        var firstResponse = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - Try to reuse the same code (replay attack)
        var secondResponse = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);

        // Assert - Must reject reused code
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var errorContent = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("invalid_grant", errorContent);
    }

    #endregion

    #region Complete OAuth Flow Tests

    [Fact]
    public async Task CompleteOAuthFlow_WithValidPKCE_ReturnsAccessAndRefreshTokens()
    {
        // Arrange
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        // Step 1: Get authorization code
        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge);
        Assert.NotNull(authCode);
        Assert.NotEmpty(authCode);

        // Step 2: Exchange code for tokens
        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = codeVerifier
        };

        var response = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        Assert.NotNull(tokens.AccessToken);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotNull(tokens.RefreshToken);
        Assert.NotEmpty(tokens.RefreshToken);
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.True(tokens.ExpiresIn > 0, "Token expiration must be positive");
    }

    [Fact]
    public async Task OAuthTokens_WorkOnProtectedEndpoints()
    {
        // Arrange - Complete OAuth flow
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge);

        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = codeVerifier
        };

        var tokenResponse = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);
        var tokens = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        // Act - Use OAuth access token on protected endpoint
        var protectedClient = _factory.CreateClient();
        protectedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await protectedClient.GetAsync("/api/bookmarks");

        // Assert - Token should work
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Client Validation Tests

    [Fact]
    public async Task AuthorizeEndpoint_WithInvalidClientId_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var url = BuildAuthorizeUrl("invalid-client-id", "http://localhost:3000/callback", codeChallenge);

        // Act
        var response = await authenticatedClient.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_client", errorContent);
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithUnauthorizedRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        // Try to redirect to attacker.com (not in OAuth client's allowed list)
        var url = BuildAuthorizeUrl("vowlt-dev-client", "https://attacker.com/steal", codeChallenge);

        // Act
        var response = await authenticatedClient.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_client", errorContent);
    }

    [Fact]
    public async Task TokenEndpoint_WithWrongClientId_ReturnsBadRequest()
    {
        // Arrange - Get code with one client
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge, "vowlt-dev-client");

        // Act - Try to exchange with different client_id
        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-chrome-extension", // DIFFERENT CLIENT!
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = codeVerifier
        };

        var response = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Token Lifetime Tests

    [Fact]
    public async Task DevClient_TokenLifetime_Is15Minutes()
    {
        // Arrange
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeVerifier = PKCEHelper.GenerateCodeVerifier();
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

        var authCode = await GetAuthorizationCodeAsync(authenticatedClient, codeChallenge, "vowlt-dev-client");

        var tokenRequest = new TokenRequest
        {
            GrantType = "authorization_code",
            Code = authCode,
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = codeVerifier
        };

        // Act
        var response = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Assert - Dev client configured for 15 minutes
        var expectedExpiresIn = 15 * 60; // 900 seconds
        Assert.True(tokens!.ExpiresIn >= expectedExpiresIn - 5, "Token lifetime should be ~15 minutes");
        Assert.True(tokens.ExpiresIn <= expectedExpiresIn + 5, "Token lifetime should be ~15 minutes");
    }

    #endregion

    #region Error Case Tests

    [Fact]
    public async Task TokenEndpoint_WithInvalidGrantType_ReturnsBadRequest()
    {
        // Act
        var tokenRequest = new TokenRequest
        {
            GrantType = "password", // Wrong! Only authorization_code supported
            Code = "dummy",
            ClientId = "vowlt-dev-client",
            RedirectUri = "http://localhost:3000/callback",
            CodeVerifier = "dummy"
        };

        var response = await _client.PostAsJsonAsync("/oauth/token", tokenRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("unsupported_grant_type", errorContent);
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithoutCodeChallengeMethod_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await AuthenticationHelper.GetAuthenticatedClientAsync(_factory);
        var codeChallenge = PKCEHelper.GenerateCodeChallenge(PKCEHelper.GenerateCodeVerifier());

        // Build URL without code_challenge_method parameter
        var url = $"/oauth/authorize" +
            $"?client_id=vowlt-dev-client" +
            $"&redirect_uri={Uri.EscapeDataString("http://localhost:3000/callback")}" +
            $"&code_challenge={codeChallenge}" +
            // Missing: &code_challenge_method=S256
            $"&response_type=code" +
            $"&state=test";

        // Act
        var response = await authenticatedClient.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private static string BuildAuthorizeUrl(string clientId, string redirectUri, string codeChallenge)
    {
        return $"/oauth/authorize" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={codeChallenge}" +
            $"&code_challenge_method=S256" +
            $"&response_type=code" +
            $"&state=test_state";
    }

    private async Task<string> GetAuthorizationCodeAsync(
        HttpClient authenticatedClient,
        string codeChallenge,
        string clientId = "vowlt-dev-client",
        string redirectUri = "http://localhost:3000/callback")
    {
        var url = BuildAuthorizeUrl(clientId, redirectUri, codeChallenge);

        var response = await authenticatedClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"));
            var authHeader = authenticatedClient.DefaultRequestHeaders.Authorization;
            throw new Exception($"Expected Redirect but got {response.StatusCode}.\nAuth: {authHeader}\nHeaders: {headers}\nResponse: {errorBody}\nURL: {url}");
        }


        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);


        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        var uri = new Uri(location!);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var code = queryParams["code"];

        Assert.NotNull(code);
        return code!;
    }

    #endregion
}

