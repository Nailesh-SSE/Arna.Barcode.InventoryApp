using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace InventoryManagement.Services.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _storage;
        private readonly ILogger<CustomAuthStateProvider> _logger;
        private const string SessionKey = "UserAuth";
        private AuthenticationState _authenticationState;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);

        public CustomAuthStateProvider(
            ProtectedSessionStorage storage,
            ILogger<CustomAuthStateProvider> logger)
        {
            _storage = storage;
            _logger = logger;
            _authenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var session = await _storage.GetAsync<AuthData>(SessionKey);

                if (session.Success && session.Value?.IsAuthenticated == true)
                {
                    var user = session.Value;

                    // Check session timeout
                    if (DateTime.UtcNow - user.LoginTime > _sessionTimeout)
                    {
                        _logger.LogInformation("Session expired for user {UserId}", user.UserId);
                        await MarkUserAsLoggedOut();
                        return _authenticationState;
                    }

                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim("FullName", user.UserFullName),
                        new Claim("LoginTime", user.LoginTime.ToString("O"))
                    }, "SessionAuth");

                    _authenticationState = new AuthenticationState(new ClaimsPrincipal(identity));
                    return _authenticationState;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
            }

            _authenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            return _authenticationState;
        }

        public async Task MarkUserAsAuthenticated(AuthData user)
        {
            try
            {
                await _storage.SetAsync(SessionKey, user);
                _logger.LogInformation("User {UserId} authenticated successfully", user.UserId);

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("FullName", user.UserFullName),
                    new Claim("LoginTime", user.LoginTime.ToString("O"))
                }, "SessionAuth");

                _authenticationState = new AuthenticationState(new ClaimsPrincipal(identity));
                NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user as authenticated");
                throw;
            }
        }

        public async Task MarkUserAsLoggedOut()
        {
            try
            {
                await _storage.DeleteAsync(SessionKey);
                _logger.LogInformation("User logged out successfully");

                _authenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user as logged out");
                throw;
            }
        }

        public async Task<bool> IsUserAuthenticatedAsync()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                return authState.User.Identity?.IsAuthenticated ?? false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUserNameAsync()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                return authState.User.Identity?.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUserFullNameAsync()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                return authState.User.FindFirst("FullName")?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // AuthData class - keep this in the same file or separate as preferred
    public class AuthData
    {
        public bool IsAuthenticated { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
    }
}
