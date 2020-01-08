using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ScopeFilteringWebApplication.Logging
{
    public class LoggerScopeSettings : ILoggerScopeSettings
    {
        private readonly IConfiguration _configuration;

        public LoggerScopeSettings(IConfiguration configuration)
        {
            _configuration = configuration;
            ChangeToken = configuration.GetReloadToken();
        }

        public IChangeToken ChangeToken { get; private set; }

        public bool IncludeScopes
        {
            get
            {
                string str = _configuration[nameof(IncludeScopes)];
                if (string.IsNullOrEmpty(str))
                    return false;
                bool result;
                if (bool.TryParse(str, out result))
                    return result;
                throw new InvalidOperationException("Configuration value '" + str + "' for setting 'IncludeScopes' is not supported.");
            }
        }

        public ILoggerScopeSettings Reload()
        {
            ChangeToken = (IChangeToken)null;
            return (ILoggerScopeSettings)new LoggerScopeSettings(_configuration);
        }

        public bool TryGetSwitch(string name, out ScopeLogLevel level)
        {
            IConfigurationSection section = _configuration.GetSection("ScopeLevel");
            if (section == null)
            {
                level = ScopeLogLevel.None;
                return false;
            }
            string str = section[name];
            if (string.IsNullOrEmpty(str))
            {
                level = ScopeLogLevel.None;
                return false;
            }
            if (Enum.TryParse<ScopeLogLevel>(str, true, out level))
                return true;
            throw new InvalidOperationException("Configuration value '" + str + "' for category '" + name + "' is not supported.");
        }
    }
}