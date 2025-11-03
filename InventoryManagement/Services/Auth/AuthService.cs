//using InventoryManagement.Services.Interfaces;
//using System.Text.Json.Serialization;
//using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

//namespace InventoryManagement.Services.Auth;

//public class AuthService
//{
//    private readonly IUserService _userService;
//    private readonly ProtectedSessionStorage _sessionStorage;
//    private const string SessionKey = "UserAuth";
//    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);

//    public AuthService(IUserService userService, ProtectedSessionStorage sessionStorage)
//    {
//        _userService = userService;
//        _sessionStorage = sessionStorage;
//    }

//    public async Task<bool> LoginAsync(string username, string password)
//    {
//        try
//        {
//            var isValid = await _userService.AuthenticateAsync(username, password);
//            if (!isValid)
//            {
//                await ClearSessionAsync();
//                return false;
//            }

//            var user = await _userService.GetUserByUsernameAsync(username);
//            if (user == null)
//            {
//                await ClearSessionAsync();
//                return false;
//            }

//            var authData = new AuthData
//            {
//                IsAuthenticated = true,
//                UserId = user.Id.ToString(),
//                UserName = user.UserName ?? username,
//                UserFullName = user.Name ?? username,
//                LoginTime = DateTime.UtcNow
//            };
                
//            await _sessionStorage.SetAsync(SessionKey, authData);
//            return true;
//        }
//        catch
//        {
//            await ClearSessionAsync();
//            return false;
//        }
//    }

//    public async Task<bool> IsAuthenticatedAsync()
//    {
//        try
//        {
//            var result = await _sessionStorage.GetAsync<AuthData>(SessionKey);
//            if (!result.Success || result.Value == null) return false;

//            var authData = result.Value;
//            return authData.IsValid();
//        }
//        catch
//        {
//            return false;
//        }
//    }

//    public async Task LogoutAsync() => await ClearSessionAsync();

//    public async Task<string?> GetCurrentUserIdAsync()
//    {
//        var authData = await GetAuthDataAsync();
//        return authData?.UserId;
//    }

//    public async Task<string?> GetCurrentUserNameAsync()
//    {
//        var authData = await GetAuthDataAsync();
//        return authData?.UserName;
//    }

//    public async Task<string?> GetCurrentUserFullNameAsync()
//    {
//        var authData = await GetAuthDataAsync();
//        return authData?.UserFullName;
//    }

//    public async Task<DateTime?> GetLoginTimeAsync()
//    {
//        var authData = await GetAuthDataAsync();
//        return authData?.LoginTime;
//    }

//    public async Task<TimeSpan> GetRemainingSessionTimeAsync()
//    {
//        var authData = await GetAuthDataAsync();
//        return authData?.RemainingTime ?? TimeSpan.Zero;
//    }

//    public async Task<bool> RefreshSessionAsync()
//    {
//        try
//        {
//            var authData = await GetAuthDataAsync();
//            if (authData?.IsValid() == true)
//            {
//                authData.Refresh();
//                await _sessionStorage.SetAsync(SessionKey, authData);
//                return true;
//            }
//            return false;
//        }
//        catch
//        {
//            return false;
//        }
//    }

//    private async Task<AuthData?> GetAuthDataAsync()
//    {
//        try
//        {
//            var result = await _sessionStorage.GetAsync<AuthData>(SessionKey);
//            return result.Success ? result.Value : null;
//        }
//        catch (InvalidOperationException) // thrown when JS interop unavailable
//        {
//            return null;
//        }
//        catch
//        {
//            return null;
//        }
//    }

//    private async Task ClearSessionAsync()
//    {
//        try
//        {
//            await _sessionStorage.DeleteAsync(SessionKey);
//        }
//        catch
//        {
//            // Ignore errors during logout
//        }
//    }
//}
//public class AuthData
//{
//    [JsonPropertyName("isAuthenticated")]
//    public bool IsAuthenticated { get; set; }

//    [JsonPropertyName("userId")]
//    public string UserId { get; set; } = string.Empty;

//    [JsonPropertyName("userName")]
//    public string UserName { get; set; } = string.Empty;

//    [JsonPropertyName("userFullName")]
//    public string UserFullName { get; set; } = string.Empty;

//    [JsonPropertyName("loginTime")]
//    public DateTime LoginTime { get; set; }

//    [JsonPropertyName("expiresAt")]
//    public DateTime ExpiresAt => LoginTime.AddHours(2); // 2-hour session

//    [JsonIgnore]
//    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

//    [JsonIgnore]
//    public TimeSpan RemainingTime => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;

//    public AuthData()
//    {
//        LoginTime = DateTime.UtcNow;
//    }

//    public AuthData(string userId, string userName, string userFullName) : this()
//    {
//        UserId = userId;
//        UserName = userName;
//        UserFullName = userFullName;
//        IsAuthenticated = true;
//    }

//    public void Refresh()
//    {
//        LoginTime = DateTime.UtcNow;
//    }

//    public bool IsValid()
//    {
//        return IsAuthenticated &&
//               !string.IsNullOrEmpty(UserId) &&
//               !string.IsNullOrEmpty(UserName) &&
//               !IsExpired;
//    }
//}
