using InventoryManagement.Services.Interfaces;

namespace InventoryManagement.Services.Auth;

public class AuthService
{
    private readonly IUserService _userService;

    private static bool _isAuthenticated = false;
    private static string _currentUserId = string.Empty;
    private static string _currentUserName = string.Empty;
    private static string _currentUserFullName = string.Empty;
    private static DateTime _loginTime;
    private static TimeSpan _sessionTimeout = TimeSpan.FromHours(2); 

    public AuthService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var isValid = await _userService.AuthenticateAsync(username, password);
            if (!isValid)
            {
                ClearSession();
                return false;
            }

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                ClearSession();
                return false;
            }

            _isAuthenticated = true;
            _currentUserId = user.Id.ToString();
            _currentUserName = user.UserName ?? username;
            _currentUserFullName = user.Name ?? username;
            _loginTime = DateTime.UtcNow;

            return true;
        }
        catch
        {
            ClearSession();
            return false;
        }
    }

    public bool IsAuthenticated()
    {
        if (!_isAuthenticated)
            return false;

        // Check session timeout    
        if (DateTime.UtcNow - _loginTime > _sessionTimeout)
        {
            // Expired session
            ClearSession();
            return false;
        }

        return true;
    }

    public void Logout() => ClearSession();

    private void ClearSession()
    {
        _isAuthenticated = false;
        _currentUserId = string.Empty;
        _currentUserName = string.Empty;
        _currentUserFullName = string.Empty;
        _loginTime = DateTime.MinValue;
    }

    public string GetCurrentUserId() => _currentUserId;
    public string GetCurrentUserName() => _currentUserName;
    public string GetCurrentUserNameFull() => _currentUserFullName;
    public DateTime? GetLoginTime() => _isAuthenticated ? _loginTime : null;
    public TimeSpan GetRemainingSessionTime()
    {
        if (!_isAuthenticated) return TimeSpan.Zero;
        var remaining = _sessionTimeout - (DateTime.UtcNow - _loginTime);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
