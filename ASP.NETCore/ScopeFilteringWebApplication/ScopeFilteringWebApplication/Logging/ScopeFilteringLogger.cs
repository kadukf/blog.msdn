using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace ScopeFilteringWebApplication.Logging
{
    internal class ScopeFilteringLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly ScopeLogLevel _scopeLevel;

        public ScopeFilteringLogger(ILogger inner, ScopeLogLevel scopeLevel)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _scopeLevel = scopeLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (_scopeLevel > ScopeLogLevel.None)
            {
                return _inner.BeginScope(state);
            }

            return NullScope.Instance;
        }
    }

    public static class ScopeFilteringLoggerExtensions
    {
        public static ILoggingBuilder UseScopeFiltering(
            this ILoggingBuilder loggingBuilder)
        {
            var services = loggingBuilder.Services;
            ServiceDescriptor defaultRegistration = services.SingleOrDefault(x => x.ServiceType == typeof(ILoggerFactory));
            if (defaultRegistration == null)
            {
                services.AddLogging();

                defaultRegistration = services.SingleOrDefault(x => x.ServiceType == typeof(ILoggerFactory));
                if (defaultRegistration == null)
                {
                    throw new Exception("Unable to find default logger factory implementation!");
                }
            }

            services.Add(ServiceDescriptor.Describe(defaultRegistration.ImplementationType, defaultRegistration.ImplementationType, ServiceLifetime.Singleton));

            services.AddSingleton(defaultRegistration);
            services.AddSingleton(p =>
            {
                var loggerFactory = (ILoggerFactory)p.GetRequiredService(defaultRegistration.ImplementationType);
                loggerFactory = new ScopeFilteringLoggerFactory(loggerFactory, p.GetRequiredService<IConfiguration>().GetSection("Logging"));
                return loggerFactory;
            });

            return loggingBuilder;
        }
    }

}