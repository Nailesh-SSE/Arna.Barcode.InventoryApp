
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace InventoryManagement.Services.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _storage;
        private readonly ILogger<CustomAuthStateProvider> _logger;
        private const string SessionKey = "UserAuth";
        private AuthenticationState _authenticationState;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);
        private AuthData? _cachedAuthData;

        public CustomAuthStateProvider(
            ProtectedLocalStorage storage,
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
                // Use cached data if available to avoid async delays
                if (_cachedAuthData?.IsAuthenticated == true)
                {
                    // Check if cache is still valid
                    if (DateTime.UtcNow - _cachedAuthData.LoginTime <= _sessionTimeout)
                    {
                        return CreateAuthenticationState(_cachedAuthData);
                    }
                    else
                    {
                        _logger.LogInformation("Cached session expired for user {UserId}", _cachedAuthData.UserId);
                        _cachedAuthData = null;
                        await MarkUserAsLoggedOut();
                        return _authenticationState;
                    }
                }

                var session = await _storage.GetAsync<AuthData>(SessionKey);

                if (session.Success && session.Value?.IsAuthenticated == true)
                {
                    var user = session.Value;
                    _cachedAuthData = user; // Cache the data

                    // Check session timeout
                    if (DateTime.UtcNow - user.LoginTime > _sessionTimeout)
                    {
                        _logger.LogInformation("Session expired for user {UserId}", user.UserId);
                        await MarkUserAsLoggedOut();
                        return _authenticationState;
                    }

                    _authenticationState = CreateAuthenticationState(user);
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

        private AuthenticationState CreateAuthenticationState(AuthData user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.UserFullName),
                new Claim("UserRoleId", user.UserRoleId),
                new Claim("LoginTime", user.LoginTime.ToString("O"))
            }, "LocalStorageAuth");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public async Task MarkUserAsAuthenticated(AuthData user)
        {
            try
            {
                await _storage.SetAsync(SessionKey, user);
                _cachedAuthData = user; // Cache the data immediately
                _logger.LogInformation("User {UserId} authenticated successfully", user.UserId);

                _authenticationState = CreateAuthenticationState(user);
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
                _cachedAuthData = null; // Clear cache
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
                // Use cached data for faster response
                if (_cachedAuthData?.IsAuthenticated == true)
                {
                    return DateTime.UtcNow - _cachedAuthData.LoginTime <= _sessionTimeout;
                }

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
                // Use cached data for faster response
                if (_cachedAuthData?.IsAuthenticated == true)
                {
                    return _cachedAuthData.UserId;
                }

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
                // Use cached data for faster response
                if (_cachedAuthData?.IsAuthenticated == true)
                {
                    return _cachedAuthData.UserName;
                }

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
                // Use cached data for faster response
                if (_cachedAuthData?.IsAuthenticated == true)
                {
                    return _cachedAuthData.UserFullName;
                }

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
        public string UserRoleId { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
    }
}
