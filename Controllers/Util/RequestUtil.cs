namespace SHAW.Controllers.Util;

public static class RequestUtil
{
    public static bool TryGetLoginKey(HttpRequest request, out string loginKey)
    {
        request.Cookies.TryGetValue("loginKey", out string? key);
        loginKey = key ?? "";
        return !string.IsNullOrEmpty(loginKey);
    }
}