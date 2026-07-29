using Hangfire.Dashboard;

namespace StreakTracker.API.Middleware;

/// <summary>
/// Hangfire dashboard'una erisimi sinirlar.
/// Dashboard job verilerini ve kullanici bilgilerini gosterdigi icin
/// disariya acik birakilmamalidir; varsayilan olarak yalnizca yerel isteklere izin verilir.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAllInDevelopment;

    public HangfireDashboardAuthorizationFilter(bool allowAllInDevelopment)
    {
        _allowAllInDevelopment = allowAllInDevelopment;
    }

    public bool Authorize(DashboardContext context)
    {
        if (_allowAllInDevelopment)
            return true;

        var httpContext = context.GetHttpContext();
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var localIp = httpContext.Connection.LocalIpAddress;

        if (remoteIp is null)
            return false;

        // Yalnizca sunucunun kendisinden gelen istekler (loopback veya sunucu IP'si) kabul edilir.
        return System.Net.IPAddress.IsLoopback(remoteIp) || remoteIp.Equals(localIp);
    }
}
