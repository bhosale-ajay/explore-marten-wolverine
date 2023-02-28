using Marten;

namespace LMS.Borrowing.API.Services;

public interface IMultiTenantReadOnlySessionFactory
{
    IQuerySession QuerySessionForTenant(string tenantId);
}

public class TenantSessionFactoryFactory: ISessionFactory, IMultiTenantReadOnlySessionFactory
{
    private readonly IDocumentStore _store;
    private readonly IEnvironmentContext _context;

    public TenantSessionFactoryFactory(IDocumentStore store, IEnvironmentContext context)
    {
        _store = store;
        _context = context;
    }

    public IQuerySession QuerySession()
    {
        return _store.QuerySession(_context.GetTenantId());
    }

    public IDocumentSession OpenSession()
    {
        return _store.LightweightSession(_context.GetTenantId());
    }
    
    public IQuerySession QuerySessionForTenant(string tenantId)
    {
        return _store.QuerySession(tenantId);
    }
}