namespace ScopeFilteringWebApplication.Logging
{
    public interface ILoggerScopeSettings
    {
        bool TryGetSwitch(string name, out ScopeLogLevel level);
    }
}