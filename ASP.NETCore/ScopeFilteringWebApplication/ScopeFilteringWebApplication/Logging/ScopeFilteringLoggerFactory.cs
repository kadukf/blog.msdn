using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ScopeFilteringWebApplication.Logging
{
    public class ScopeFilteringLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _inner;
        private readonly IConfiguration _cfg;

        public ScopeFilteringLoggerFactory(ILoggerFactory inner, IConfiguration cfg)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ScopeFilteringLogger(_inner.CreateLogger(categoryName), GetLevel(categoryName, new LoggerScopeSettings(_cfg)));
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _inner.AddProvider(provider);
        }

        private ScopeLogLevel GetLevel(string name, ILoggerScopeSettings settings)
        {
            ////if (this._filter != null)
            ////    return this._filter;
            if (settings != null)
            {
                foreach (string keyPrefix in GetKeyPrefixes(name))
                {
                    if (settings.TryGetSwitch(keyPrefix, out var level))
                    {
                        return level;
                    }
                }
            }
            return ScopeLogLevel.None;
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            int length;
            for (; !string.IsNullOrEmpty(name); name = name.Substring(0, length))
            {
                yield return name;
                length = name.LastIndexOf('.');
                if (length == -1)
                {
                    yield return "Default";
                    break;
                }
            }
        }
    }
}