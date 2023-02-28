namespace LMS.Borrowing.API.Services;

public interface IEnvironmentContext
{
    public string GetTenantId();

    public IEnumerable<string> GetActiveTenants();
}

public class EnvironmentContext : IEnvironmentContext
{
    private readonly string _tenantId;
    public EnvironmentContext(IHttpContextAccessor httpContextAccessor)
    {
        this._tenantId = httpContextAccessor.HttpContext.Request.Host.Host;
    }

    public string GetTenantId()
    {
        return _tenantId;
    }

    public IEnumerable<string> GetActiveTenants()
    {
        yield return "mason.lms.com";
        yield return "cin.lms.com";
    }
}