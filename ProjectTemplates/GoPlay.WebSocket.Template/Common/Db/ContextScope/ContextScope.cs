using Microsoft.Extensions.DependencyInjection;

namespace GoPlayProj.Database;

public class ContextScope : IDisposable
{
    private IServiceScope _serviceScope;
    private GoPlayProjContext _GoPlayProjContext;

    public GoPlayProjContext DbContext => _GoPlayProjContext;
    
    public ContextScope(IServiceScope scope, GoPlayProjContext context)
    {
        _serviceScope = scope;
        _GoPlayProjContext = context;
    }
    
    public void Dispose()
    {
        _GoPlayProjContext.Dispose();
        _serviceScope.Dispose();
    }
}