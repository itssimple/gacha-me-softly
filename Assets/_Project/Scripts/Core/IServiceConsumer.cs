namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Scene components that need Core services implement this;
    /// <see cref="GameBootstrap"/> discovers them once in the Boot scene and
    /// injects the registry during its Awake. This keeps injection at the
    /// composition root — consumers never look services up globally
    /// (CLAUDE.md "No singletons").
    /// </summary>
    public interface IServiceConsumer
    {
        void InitServices(ServiceRegistry services);
    }
}
